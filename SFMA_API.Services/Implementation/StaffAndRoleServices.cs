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
                || roles.Contains("academic_head");

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

        public static async Task ValidateTeacherClassAccessAsync(
            IUnitOfWork unitOfWork,
            ClaimsPrincipal currentUser,
            Guid targetClassId)
        {
            if (currentUser == null) return;

            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSupervisor = roles.Contains("super_admin") || roles.Contains("academic_admin") || roles.Contains("academic_head");
            if (isSupervisor || !roles.Contains("teacher"))
            {
                return;
            }

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId)) return;

            var staff = await unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId);
            if (staff == null) return;

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
                || roles.Contains("financial_head");

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
            }
        }
    }

    public class StaffService : IStaffService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IMapper _mapper;
        private readonly ISchoolNotificationService _notificationService;

        public StaffService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IMapper mapper,
            ISchoolNotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        private async Task<(Guid? ClassId, string? ClassName)> AssignClassToStaffAsync(Guid staffId, Guid? classSectionId, string? className)
        {
            var classRepo = _unitOfWork.GetRepository<ClassSection>();
            var previouslyAssigned = await classRepo.GetByAsync(c => c.HomeroomTeacherId == staffId);
            foreach (var prev in previouslyAssigned)
            {
                if (!classSectionId.HasValue || prev.Id != classSectionId.Value)
                {
                    prev.HomeroomTeacherId = null;
                    prev.UpdatedAt = DateTime.UtcNow;
                    await classRepo.UpdateAsync(prev);
                }
            }

            if (!classSectionId.HasValue || classSectionId.Value == Guid.Empty)
            {
                return (null, null);
            }

            var existingSection = await classRepo.GetByIdAsync(classSectionId.Value);
            var targetSection = await ClassSectionHelper.EnsureClassSectionAsync(_unitOfWork, classSectionId.Value, className);
            targetSection.HomeroomTeacherId = staffId;
            targetSection.UpdatedAt = DateTime.UtcNow;

            if (existingSection != null)
            {
                await classRepo.UpdateAsync(targetSection);
            }

            return (targetSection.Id, targetSection.Name);
        }

        public async Task<PagedResponse<StaffResponse>> GetAllStaff(string? department, string? roleKey, string? status, RequestParameters parameters)
        {
            var staffRepo = _unitOfWork.GetRepository<Staff>();
            var query = staffRepo.GetQueryable(include: q => q.Include(s => s.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role));

            if (!string.IsNullOrWhiteSpace(department))
                query = query.Where(s => s.Department == department);

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<UserStatus>(status, true, out var userStatus))
                    query = query.Where(s => s.User.Status == userStatus);
            }

            if (!string.IsNullOrWhiteSpace(roleKey))
                query = query.Where(s => s.User.UserRoles.Any(ur => ur.Role.Key == roleKey || ur.Role.Name == roleKey));

            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var term = parameters.SearchTerm.Trim().ToLower();
                query = query.Where(s => s.StaffCode.ToLower().Contains(term) || s.User.DisplayName.ToLower().Contains(term) || s.User.Email!.ToLower().Contains(term));
            }

            var count = await query.CountAsync();
            var skip = (parameters.PageNumber - 1) * parameters.PageSize;
            var items = await query.OrderBy(s => s.StaffCode).Skip(skip).Take(parameters.PageSize).ToListAsync();

            var mappedItems = _mapper.Map<List<StaffResponse>>(items);
            var assignedClasses = await _unitOfWork.GetRepository<ClassSection>().GetByAsync(c => c.HomeroomTeacherId != null);

            foreach (var dto in mappedItems)
            {
                var assigned = assignedClasses.FirstOrDefault(c => c.HomeroomTeacherId == dto.Id);
                if (assigned != null)
                {
                    dto.AssignedClassId = assigned.Id;
                    dto.AssignedClassName = assigned.Name;
                }
            }

            return new PagedResponse<StaffResponse>
            {
                MetaData = new MetaData
                {
                    TotalCount = count,
                    CurrentPage = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    TotalPages = (int)Math.Ceiling(count / (double)parameters.PageSize)
                },
                Items = mappedItems
            };
        }

        public async Task<StaffResponse?> GetStaffById(Guid id)
        {
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(
                s => s.Id == id,
                include: q => q.Include(s => s.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role));

            if (staff == null) return null;

            var dto = _mapper.Map<StaffResponse>(staff);
            var assigned = await _unitOfWork.GetRepository<ClassSection>().GetSingleByAsync(c => c.HomeroomTeacherId == staff.Id);
            if (assigned != null)
            {
                dto.AssignedClassId = assigned.Id;
                dto.AssignedClassName = assigned.Name;
            }
            return dto;
        }

        public async Task<CreateStaffResponse> CreateStaff(CreateStaffRequest request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email.Trim().ToLower());
            if (existingUser != null)
            {
                throw new InvalidOperationException($"User with email '{request.Email}' already exists.");
            }

            string tempPassword = $"Staff@{Guid.NewGuid().ToString("N")[..6]}!";

            var user = new ApplicationUser
            {
                Email = request.Email.Trim().ToLower(),
                UserName = request.Email.Trim().ToLower(),
                DisplayName = request.FullName,
                PhoneNumber = request.Phone,
                Status = UserStatus.Active,
                Active = true,
                EmailConfirmed = false
            };

            var userRes = await _userManager.CreateAsync(user, tempPassword);
            if (!userRes.Succeeded)
            {
                throw new InvalidOperationException(string.Join("\n", userRes.Errors.Select(e => e.Description)));
            }

            // Generate email confirmation token for auto-verification URL
            string verificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            if (!await _roleManager.RoleExistsAsync(request.RoleKey))
            {
                await _roleManager.CreateAsync(new ApplicationRole(request.RoleKey));
            }

            await _userManager.AddToRoleAsync(user, request.RoleKey);

            var staffCount = await _unitOfWork.GetRepository<Staff>().CountAsync();
            string staffCode = $"STF-{(staffCount + 1):D3}";

            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StaffCode = staffCode,
                Department = request.Department,
                Title = request.Title,
                Phone = request.Phone,
                JoinedDate = DateTime.UtcNow.Date,
                User = user
            };

            await _unitOfWork.GetRepository<Staff>().AddAsync(staff);

            (Guid? assignedClassId, string? assignedClassName) = (null, null);
            if (request.AssignedClassId.HasValue && request.AssignedClassId.Value != Guid.Empty)
            {
                (assignedClassId, assignedClassName) = await AssignClassToStaffAsync(staff.Id, request.AssignedClassId, request.AssignedClassName);
            }

            await _unitOfWork.SaveChangesAsync();

            var staffResponse = _mapper.Map<StaffResponse>(staff);
            staffResponse.Role = request.RoleKey;
            staffResponse.AssignedClassId = assignedClassId;
            staffResponse.AssignedClassName = assignedClassName;

            var userResponse = _mapper.Map<UserProfileResponse>(user);
            userResponse.Role = request.RoleKey;
            userResponse.AssignedClassId = assignedClassId;
            userResponse.AssignedClassName = assignedClassName;

            // Background notification dispatch to staff email and phone
            _ = Task.Run(() => _notificationService.SendStaffCredentialsAsync(
                request.FullName,
                request.Email,
                request.Phone,
                staffCode,
                request.Department,
                request.RoleKey,
                tempPassword,
                verificationToken));

            return new CreateStaffResponse
            {
                Staff = staffResponse,
                User = userResponse,
                TempPassword = tempPassword
            };
        }

        public async Task<StaffResponse> UpdateStaff(Guid id, UpdateStaffRequest request)
        {
            var staffRepo = _unitOfWork.GetRepository<Staff>();
            var staff = await staffRepo.GetSingleByAsync(s => s.Id == id, include: q => q.Include(s => s.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role));
            if (staff == null)
            {
                throw new KeyNotFoundException("Staff record not found");
            }

            staff.Department = request.Department;
            staff.Title = request.Title;
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                staff.Phone = request.Phone;
                staff.User.PhoneNumber = request.Phone;
            }

            if (!string.IsNullOrWhiteSpace(request.RoleKey))
            {
                var currentRoles = await _userManager.GetRolesAsync(staff.User);
                await _userManager.RemoveFromRolesAsync(staff.User, currentRoles);
                if (!await _roleManager.RoleExistsAsync(request.RoleKey))
                {
                    await _roleManager.CreateAsync(new ApplicationRole(request.RoleKey));
                }
                await _userManager.AddToRoleAsync(staff.User, request.RoleKey);
            }

            if (request.AssignedClassId.HasValue)
            {
                await AssignClassToStaffAsync(staff.Id, request.AssignedClassId, request.AssignedClassName);
            }
            else if (!string.IsNullOrWhiteSpace(request.RoleKey) && !request.RoleKey.Equals("teacher", StringComparison.OrdinalIgnoreCase))
            {
                await AssignClassToStaffAsync(staff.Id, null, null);
            }

            await staffRepo.UpdateAsync(staff);
            await _userManager.UpdateAsync(staff.User);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<StaffResponse>(staff);
            if (!string.IsNullOrWhiteSpace(request.RoleKey))
            {
                dto.Role = request.RoleKey;
            }
            var assigned = await _unitOfWork.GetRepository<ClassSection>().GetSingleByAsync(c => c.HomeroomTeacherId == staff.Id);
            if (assigned != null)
            {
                dto.AssignedClassId = assigned.Id;
                dto.AssignedClassName = assigned.Name;
            }
            return dto;
        }

        public async Task<StaffResponse> UpdateStaffStatus(Guid id, UpdateStaffStatusRequest request)
        {
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.Id == id, include: q => q.Include(s => s.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role));
            if (staff == null)
            {
                throw new KeyNotFoundException("Staff record not found");
            }

            if (request.Status.Equals("suspended", StringComparison.OrdinalIgnoreCase))
            {
                staff.User.Status = UserStatus.Suspended;
                await _userManager.RemoveAuthenticationTokenAsync(staff.User, "SFMA", "RefreshToken");
            }
            else
            {
                staff.User.Status = UserStatus.Active;
            }

            staff.User.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(staff.User);

            return _mapper.Map<StaffResponse>(staff);
        }

        public async Task<string> AdminResetPassword(Guid id)
        {
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.Id == id, include: q => q.Include(s => s.User));
            if (staff == null)
            {
                throw new KeyNotFoundException("Staff record not found");
            }

            string newPassword = $"Staff@{Guid.NewGuid().ToString("N")[..6]}!";
            var token = await _userManager.GeneratePasswordResetTokenAsync(staff.User);
            var res = await _userManager.ResetPasswordAsync(staff.User, token, newPassword);
            if (!res.Succeeded)
            {
                throw new InvalidOperationException(string.Join("\n", res.Errors.Select(e => e.Description)));
            }

            // Dispatch notification to staff
            _ = Task.Run(() => _notificationService.SendStaffPasswordResetAsync(
                staff.User.DisplayName,
                staff.User.Email ?? string.Empty,
                staff.Phone,
                newPassword));

            return newPassword;
        }
    }
}
