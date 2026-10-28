using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SFMA_API.Data.Interfaces;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using SFMA_API.Models.Enums;
using SFMA_API.Services.Exceptions;
using SFMA_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SFMA_API.Services.Implementation
{
    public static class ClassSectionHelper
    {
        public static async Task<ClassSection> EnsureClassSectionAsync(
            IUnitOfWork unitOfWork,
            Guid classSectionId,
            string? explicitName = null,
            Guid? explicitTermId = null)
        {
            var classRepo = unitOfWork.GetRepository<ClassSection>();

            if (classSectionId != Guid.Empty)
            {
                var existing = await classRepo.GetByIdAsync(classSectionId);
                if (existing != null)
                {
                    if (!string.IsNullOrWhiteSpace(explicitName) && existing.Name != explicitName.Trim())
                    {
                        existing.Name = explicitName.Trim();
                        existing.UpdatedAt = DateTime.UtcNow;
                        await classRepo.UpdateAsync(existing);
                    }
                    return existing;
                }
            }

            if (!string.IsNullOrWhiteSpace(explicitName))
            {
                var byName = await classRepo.GetSingleByAsync(
                    c => c.Name.ToLower() == explicitName.Trim().ToLower());
                if (byName != null) return byName;
            }

            if (string.IsNullOrWhiteSpace(explicitName))
            {
                throw new KeyNotFoundException($"Class section with ID '{classSectionId}' was not found.");
            }

            Guid termId;
            if (explicitTermId.HasValue && explicitTermId.Value != Guid.Empty)
            {
                termId = explicitTermId.Value;
            }
            else
            {
                var activeTerm = await unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => t.IsActive && t.Active)
                    ?? await unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => true);

                if (activeTerm == null)
                {
                    throw new InvalidOperationException("No academic term is configured. Please create an academic term in Session & Terms Setup first.");
                }
                termId = activeTerm.Id;
            }

            var newSection = new ClassSection
            {
                Id = classSectionId != Guid.Empty ? classSectionId : Guid.NewGuid(),
                Name = explicitName.Trim(),
                Division = "General",
                AcademicTermId = termId,
                Active = true,
                CreatedAt = DateTime.UtcNow
            };

            await classRepo.AddAsync(newSection);
            return newSection;
        }

        public static void ValidateCurriculumMutationAccess(ClaimsPrincipal? currentUser, string requiredClaim)
        {
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Access denied: Authenticated user context is required to modify curriculum subjects.");
            }

            var roles = currentUser.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            bool isAuthorizedRole = roles.Contains("super_admin")
                || roles.Contains("academic_admin")
                || roles.Contains("academic_head")
                || roles.Contains("admin");

            bool hasPermissionClaim = !string.IsNullOrWhiteSpace(requiredClaim)
                && currentUser.Claims.Any(c =>
                    (c.Type == "Permission" || c.Type == "permission" || c.Type == "claims")
                    && (string.Equals(c.Value, "all", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.Value, requiredClaim, StringComparison.OrdinalIgnoreCase)));

            if (!isAuthorizedRole && !hasPermissionClaim)
            {
                throw new UnauthorizedAccessException("Access denied: Only academic administrators or head of school can create, update, or delete curriculum subjects.");
            }
        }

        public static void ValidateAdministrativeMutationAccess(ClaimsPrincipal? currentUser, string requiredClaim, string resourceName = "this resource")
        {
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException($"Access denied: Authenticated user context is required to modify {resourceName}.");
            }

            var roles = currentUser.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            bool isAuthorizedRole = roles.Contains("super_admin")
                || roles.Contains("academic_admin")
                || roles.Contains("academic_head")
                || roles.Contains("admin");

            bool hasPermissionClaim = !string.IsNullOrWhiteSpace(requiredClaim)
                && currentUser.Claims.Any(c =>
                    (c.Type == "Permission" || c.Type == "permission" || c.Type == "claims")
                    && (string.Equals(c.Value, "all", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.Value, requiredClaim, StringComparison.OrdinalIgnoreCase)));

            if (!isAuthorizedRole && !hasPermissionClaim)
            {
                throw new UnauthorizedAccessException($"Access denied: Only administrators or academic heads can create, update, or delete {resourceName}.");
            }
        }

        public static async Task ValidateTeacherClassAccessAsync(
            IUnitOfWork unitOfWork,
            ClaimsPrincipal currentUser,
            Guid targetClassId)
        {
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Access denied: Authenticated user context is required.");
            }

            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSupervisor = roles.Contains("super_admin") || roles.Contains("academic_admin") || roles.Contains("academic_head") || roles.Contains("admin");
            if (isSupervisor)
            {
                return;
            }

            if (!roles.Contains("teacher"))
            {
                throw new UnauthorizedAccessException("Access denied: Only assigned teachers or academic supervisors can perform this operation.");
            }

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Access denied: Unable to resolve user identity.");
            }

            var staff = await unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId);
            if (staff == null)
            {
                throw new UnauthorizedAccessException("Access denied: No active staff profile found for this user.");
            }

            var assignedClass = await unitOfWork.GetRepository<ClassSection>().GetSingleByAsync(c => c.HomeroomTeacherId == staff.Id);
            if (assignedClass != null && assignedClass.Id != targetClassId)
            {
                throw new UnauthorizedAccessException($"Access denied: You are assigned solely to {assignedClass.Name} and cannot modify records for another class.");
            }
        }

        public static async Task<Student> ValidateStudentOwnershipAsync(
            IUnitOfWork unitOfWork,
            ClaimsPrincipal currentUser,
            Guid studentId)
        {
            if (studentId == Guid.Empty)
            {
                throw new ArgumentException("A valid student identifier is required.");
            }

            var student = await unitOfWork.GetRepository<Student>().GetSingleByAsync(
                s => s.Id == studentId,
                include: q => q.Include(s => s.ClassSection)
                               .Include(s => s.StudentParents).ThenInclude(sp => sp.Parent));

            if (student == null)
            {
                throw new KeyNotFoundException("Student record not found.");
            }

            if (currentUser == null) return student;

            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isStaffOrAdmin = roles.Contains("super_admin")
                || roles.Contains("academic_admin")
                || roles.Contains("financial_admin")
                || roles.Contains("academic_head")
                || roles.Contains("financial_head")
                || roles.Contains("teacher");

            if (isStaffOrAdmin)
            {
                if (roles.Contains("teacher") && !roles.Contains("super_admin") && !roles.Contains("academic_admin") && !roles.Contains("academic_head"))
                {
                    await ValidateTeacherClassAccessAsync(unitOfWork, currentUser, student.ClassSectionId);
                }
                return student;
            }

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Access denied: Unable to verify caller identity.");
            }

            if (roles.Contains("student"))
            {
                if (!string.Equals(student.UserId, userId, StringComparison.Ordinal))
                {
                    throw new UnauthorizedAccessException("Access denied: Students may only access their own academic and financial records.");
                }
                return student;
            }

            if (roles.Contains("parent"))
            {
                var parent = await unitOfWork.GetRepository<Parent>().GetSingleByAsync(p => p.UserId == userId);
                bool isLinkedWard = parent != null && student.StudentParents.Any(sp => sp.ParentId == parent.Id);
                if (!isLinkedWard)
                {
                    throw new UnauthorizedAccessException("Access denied: Parents may only access records of their linked wards.");
                }
                return student;
            }

            throw new UnauthorizedAccessException("Access denied: Insufficient permissions to view this student record.");
        }

        public static async Task ValidateClassAccessForPortalUserAsync(
            IUnitOfWork unitOfWork,
            ClaimsPrincipal currentUser,
            Guid classSectionId)
        {
            if (currentUser == null || classSectionId == Guid.Empty) return;

            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSupervisor = roles.Contains("super_admin")
                || roles.Contains("academic_admin")
                || roles.Contains("financial_admin")
                || roles.Contains("academic_head")
                || roles.Contains("financial_head")
                || roles.Contains("admin");

            if (isSupervisor) return;

            if (roles.Contains("teacher"))
            {
                await ValidateTeacherClassAccessAsync(unitOfWork, currentUser, classSectionId);
                return;
            }

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Access denied: Unable to verify caller identity.");
            }

            if (roles.Contains("student"))
            {
                var student = await unitOfWork.GetRepository<Student>().GetSingleByAsync(s => s.UserId == userId);
                if (student == null || student.ClassSectionId != classSectionId)
                {
                    throw new UnauthorizedAccessException("Access denied: Students may only view resources for their assigned class.");
                }
                return;
            }

            if (roles.Contains("parent"))
            {
                var parent = await unitOfWork.GetRepository<Parent>().GetSingleByAsync(
                    p => p.UserId == userId,
                    include: q => q.Include(p => p.StudentParents).ThenInclude(sp => sp.Student));
                bool hasWardInClass = parent != null && parent.StudentParents.Any(sp => sp.Student != null && sp.Student.ClassSectionId == classSectionId);
                if (!hasWardInClass)
                {
                    throw new UnauthorizedAccessException("Access denied: Parents may only view resources for classes containing their linked wards.");
                }
                return;
            }

            throw new UnauthorizedAccessException("Access denied: You do not have permission to access resources for this class.");
        }
    }

   
}
