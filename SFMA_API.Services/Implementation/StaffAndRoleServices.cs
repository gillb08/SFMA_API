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

            var mappedItems = _mapper.Map<IEnumerable<StaffResponse>>(items);
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

            return staff != null ? _mapper.Map<StaffResponse>(staff) : null;
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
                EmailConfirmed = true
            };

            var userRes = await _userManager.CreateAsync(user, tempPassword);
            if (!userRes.Succeeded)
            {
                throw new InvalidOperationException(string.Join("\n", userRes.Errors.Select(e => e.Description)));
            }

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
            await _unitOfWork.SaveChangesAsync();

            var staffResponse = _mapper.Map<StaffResponse>(staff);
            var userResponse = _mapper.Map<UserProfileResponse>(user);
            userResponse.Role = request.RoleKey;

            // Background notification dispatch to staff email and phone
            _ = Task.Run(() => _notificationService.SendStaffCredentialsAsync(
                request.FullName,
                request.Email,
                request.Phone,
                staffCode,
                request.Department,
                request.RoleKey,
                tempPassword));

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

            await staffRepo.UpdateAsync(staff);
            await _userManager.UpdateAsync(staff.User);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<StaffResponse>(staff);
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
