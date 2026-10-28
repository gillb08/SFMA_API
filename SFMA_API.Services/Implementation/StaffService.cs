using AutoMapper;
using Microsoft.AspNetCore.Identity;
using SFMA_API.Data.Interfaces;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using SFMA_API.Models.Enums;
using SFMA_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace SFMA_API.Services.Implementation
{
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

            var targetSection = await ClassSectionHelper.EnsureClassSectionAsync(_unitOfWork, classSectionId.Value, className);
            targetSection.HomeroomTeacherId = staffId;
            targetSection.UpdatedAt = DateTime.UtcNow;
            await classRepo.UpdateAsync(targetSection);

            return (targetSection.Id, targetSection.Name);
        }

        private static IQueryable<StaffResponse> ProjectStaff(IQueryable<Staff> query)
        {
            return query.Select(s => new StaffResponse
            {
                Id = s.Id,
                UserId = s.UserId,
                StaffCode = s.StaffCode,
                Department = s.Department,
                Title = s.Title,
                Phone = s.Phone,
                JoinedDate = s.JoinedDate,
                FullName = s.User != null ? s.User.DisplayName : string.Empty,
                Email = s.User != null && s.User.Email != null ? s.User.Email : string.Empty,
                Role = s.User != null
                    ? (s.User.UserRoles.Select(ur => ur.Role.Key).FirstOrDefault() ?? string.Empty)
                    : string.Empty,
                Status = s.User != null ? s.User.Status.ToString() : "Active"
            });
        }

        public async Task<PagedResponse<StaffResponse>> GetAllStaff(string? department, string? roleKey, string? status, RequestParameters parameters)
        {
            var staffRepo = _unitOfWork.GetRepository<Staff>();
            var query = staffRepo.GetQueryable();

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
            var mappedItems = await ProjectStaff(query.OrderBy(s => s.StaffCode).Skip(skip).Take(parameters.PageSize)).ToListAsync();

            var assignedClasses = await _unitOfWork.GetRepository<ClassSection>().GetQueryable(c => c.HomeroomTeacherId != null)
                .Select(c => new { c.Id, c.Name, c.HomeroomTeacherId })
                .ToListAsync();

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
            var dto = await ProjectStaff(_unitOfWork.GetRepository<Staff>().GetQueryable(s => s.Id == id))
                .FirstOrDefaultAsync();

            if (dto == null) return null;

            var assigned = await _unitOfWork.GetRepository<ClassSection>().GetQueryable(c => c.HomeroomTeacherId == id)
                .Select(c => new { c.Id, c.Name })
                .FirstOrDefaultAsync();
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
                JoinedDate = DateTime.UtcNow.Date
            };

            await _unitOfWork.GetRepository<Staff>().AddAsync(staff);

            (Guid? assignedClassId, string? assignedClassName) = (null, null);
            if (request.AssignedClassId.HasValue && request.AssignedClassId.Value != Guid.Empty)
            {
                (assignedClassId, assignedClassName) = await AssignClassToStaffAsync(staff.Id, request.AssignedClassId, request.AssignedClassName);
            }

            await _unitOfWork.SaveChangesAsync();

            var staffResponse = (await GetStaffById(staff.Id)) ?? new StaffResponse
            {
                Id = staff.Id,
                UserId = user.Id,
                StaffCode = staffCode,
                Department = staff.Department,
                Title = staff.Title,
                Phone = staff.Phone,
                JoinedDate = staff.JoinedDate,
                FullName = user.DisplayName,
                Email = user.Email ?? string.Empty,
                Role = request.RoleKey,
                Status = user.Status.ToString(),
                AssignedClassId = assignedClassId,
                AssignedClassName = assignedClassName
            };

            var userResponse = new UserProfileResponse
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                Role = request.RoleKey,
                StaffId = staff.Id,
                AssignedClassId = assignedClassId,
                AssignedClassName = assignedClassName
            };

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
            var staff = await staffRepo.GetByIdAsync(id);
            if (staff == null)
            {
                throw new KeyNotFoundException("Staff record not found");
            }

            var user = await _userManager.FindByIdAsync(staff.UserId);
            if (user == null)
            {
                throw new KeyNotFoundException("Associated staff user account not found");
            }

            staff.Department = request.Department;
            staff.Title = request.Title;
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                staff.Phone = request.Phone;
                user.PhoneNumber = request.Phone;
            }

            if (!string.IsNullOrWhiteSpace(request.RoleKey))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }
                if (!await _roleManager.RoleExistsAsync(request.RoleKey))
                {
                    await _roleManager.CreateAsync(new ApplicationRole(request.RoleKey));
                }
                await _userManager.AddToRoleAsync(user, request.RoleKey);
            }

            if (request.AssignedClassId.HasValue)
            {
                await AssignClassToStaffAsync(staff.Id, request.AssignedClassId, request.AssignedClassName);
            }
            else if (!string.IsNullOrWhiteSpace(request.RoleKey) && !request.RoleKey.Equals("teacher", StringComparison.OrdinalIgnoreCase))
            {
                await AssignClassToStaffAsync(staff.Id, null, null);
            }

            staff.UpdatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await staffRepo.UpdateAsync(staff);
            await _userManager.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var dto = (await GetStaffById(staff.Id))!;
            if (!string.IsNullOrWhiteSpace(request.RoleKey))
            {
                dto.Role = request.RoleKey;
            }
            return dto;
        }

        public async Task<StaffResponse> UpdateStaffStatus(Guid id, UpdateStaffStatusRequest request)
        {
            var staff = await _unitOfWork.GetRepository<Staff>().GetByIdAsync(id);
            if (staff == null)
            {
                throw new KeyNotFoundException("Staff record not found");
            }

            var user = await _userManager.FindByIdAsync(staff.UserId);
            if (user == null)
            {
                throw new KeyNotFoundException("Associated staff user account not found");
            }

            if (request.Status.Equals("suspended", StringComparison.OrdinalIgnoreCase))
            {
                user.Status = UserStatus.Suspended;
                await _userManager.RemoveAuthenticationTokenAsync(user, "SFMA", "RefreshToken");
            }
            else
            {
                user.Status = UserStatus.Active;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return (await GetStaffById(staff.Id))!;
        }

        public async Task<string> AdminResetPassword(Guid id)
        {
            var staff = await _unitOfWork.GetRepository<Staff>().GetByIdAsync(id);
            if (staff == null)
            {
                throw new KeyNotFoundException("Staff record not found");
            }

            var user = await _userManager.FindByIdAsync(staff.UserId);
            if (user == null)
            {
                throw new KeyNotFoundException("Associated staff user account not found");
            }

            string newPassword = $"Staff@{Guid.NewGuid().ToString("N")[..6]}!";
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var res = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!res.Succeeded)
            {
                throw new InvalidOperationException(string.Join("\n", res.Errors.Select(e => e.Description)));
            }

            // Dispatch notification to staff
            _ = Task.Run(() => _notificationService.SendStaffPasswordResetAsync(
                user.DisplayName,
                user.Email ?? string.Empty,
                staff.Phone,
                newPassword));

            return newPassword;
        }
    }
}
