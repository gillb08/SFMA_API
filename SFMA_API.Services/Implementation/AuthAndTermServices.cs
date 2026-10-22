using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SFMA_API.Data.Interfaces;
using SFMA_API.Models.Configuration;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using SFMA_API.Models.Enums;
using SFMA_API.Services.Infrastructure;
using SFMA_API.Services.Interfaces;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SFMA_API.Services.Implementation
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJWTAuthenticator _jwtAuthenticator;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly SchoolSettings _schoolSettings;
        private readonly IMenuService _menuService;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            IJWTAuthenticator jwtAuthenticator,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IOptions<SchoolSettings> schoolSettings,
            IMenuService menuService)
        {
            _userManager = userManager;
            _jwtAuthenticator = jwtAuthenticator;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _schoolSettings = schoolSettings.Value;
            _menuService = menuService;
        }

        public async Task<LoggedInUserResponse> Login(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email.Trim().ToLower())
                ?? await _userManager.FindByNameAsync(request.Email.Trim().ToLower());

            if (user == null || !user.Active)
            {
                throw new InvalidOperationException("Invalid email or password");
            }

            if (user.Status == UserStatus.Suspended)
            {
                throw new UnauthorizedAccessException("Account is suspended");
            }

            if (user.Status == UserStatus.Locked)
            {
                throw new UnauthorizedAccessException("Account is locked");
            }

            bool passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                throw new InvalidOperationException("Invalid email or password");
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            string primaryRole = roles.FirstOrDefault()
                ?? throw new InvalidOperationException("User account has no assigned role. Contact your administrator.");

            var token = await _jwtAuthenticator.GenerateJwtToken(user);
            var refreshToken = await _jwtAuthenticator.GenerateRefreshToken(user);

            // Portal route resolved from configuration — no hardcoded paths in code
            string portalRedirect = _schoolSettings.GetPortalRoute(primaryRole);
            var (assignedClassId, assignedClassName) = await ResolveUserClassAssignmentAsync(user.Id);

            var userProfile = new UserProfileResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                Role = primaryRole,
                AvatarUrl = user.AvatarUrl,
                PhoneNumber = user.PhoneNumber,
                PortalRedirect = portalRedirect,
                AssignedClassId = assignedClassId,
                AssignedClassName = assignedClassName
            };

            var (menuItems, permissions) = await GetUserNavigationAndPermissions(user.Id, roles);

            return new LoggedInUserResponse
            {
                Token = token.Token,
                RefreshToken = refreshToken,
                User = userProfile,
                MenuItems = menuItems,
                Permissions = permissions
            };
        }

        private async Task<(Guid? ClassId, string? ClassName)> ResolveUserClassAssignmentAsync(string userId)
        {
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId);
            if (staff != null)
            {
                var assignedClass = await _unitOfWork.GetRepository<ClassSection>().GetSingleByAsync(c => c.HomeroomTeacherId == staff.Id);
                if (assignedClass != null)
                {
                    return (assignedClass.Id, assignedClass.Name);
                }
            }

            var student = await _unitOfWork.GetRepository<Student>().GetSingleByAsync(
                s => s.UserId == userId,
                include: q => q.Include(st => st.ClassSection));
            if (student != null)
            {
                return (student.ClassSectionId, student.ClassSection?.Name);
            }

            return (null, null);
        }

        private async Task<(List<string> MenuItems, List<string> Permissions)> GetUserNavigationAndPermissions(string userId, IList<string> roles)
        {
            var userRoleRepo = _unitOfWork.GetRepository<ApplicationUserRole>();
            var userClaimRepo = _unitOfWork.GetRepository<ApplicationUserClaim>();

            var userRoles = await userRoleRepo.GetQueryable(
                include: q => q.Include(ur => ur.Role).ThenInclude(r => r.RoleClaims))
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            var directClaims = await userClaimRepo.GetByAsync(r => r.UserId == userId && r.Active);

            var permissionsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool isSuperAdmin = roles.Contains("super_admin") || roles.Contains("superadmin");

            if (isSuperAdmin)
            {
                permissionsSet.Add("all");
            }

            // Baseline session permissions for every authenticated user
            permissionsSet.Add("logout");
            permissionsSet.Add("get-current-user-profile");
            permissionsSet.Add("change-password");
            permissionsSet.Add("update-user-profile");
            permissionsSet.Add("get-active-term");
            permissionsSet.Add("get-all-terms");

            foreach (var ur in userRoles)
            {
                if (ur.Role.Active)
                {
                    foreach (var rc in ur.Role.RoleClaims)
                    {
                        if (rc.Active && !string.IsNullOrWhiteSpace(rc.ClaimValue))
                        {
                            permissionsSet.Add(rc.ClaimValue.Trim().ToLowerInvariant());
                        }
                    }
                }
            }

            foreach (var dc in directClaims)
            {
                if (!string.IsNullOrWhiteSpace(dc.ClaimValue))
                {
                    permissionsSet.Add(dc.ClaimValue.Trim().ToLowerInvariant());
                }
            }

            var menuItems = await _menuService.GetMenuItems(permissionsSet);
            return (menuItems.ToList(), permissionsSet.OrderBy(p => p).ToList());
        }

        public async Task<bool> Logout(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Token provider name read from configuration, not hardcoded
                await _userManager.RemoveAuthenticationTokenAsync(user, _schoolSettings.TokenProviderName, "RefreshToken");
            }
            return true;
        }

        public async Task<UserProfileResponse> GetCurrentUserProfile(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var roles = await _userManager.GetRolesAsync(user);
            string primaryRole = roles.FirstOrDefault()
                ?? throw new InvalidOperationException("User account has no assigned role. Contact your administrator.");

            string? address = null;
            string? phone = user.PhoneNumber;
            var parent = await _unitOfWork.GetRepository<Parent>().GetSingleByAsync(p => p.UserId == userId);
            if (parent != null)
            {
                address = parent.Address;
                phone = string.IsNullOrWhiteSpace(phone) ? parent.Phone : phone;
            }

            var (assignedClassId, assignedClassName) = await ResolveUserClassAssignmentAsync(userId);

            return new UserProfileResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                Role = primaryRole,
                AvatarUrl = user.AvatarUrl,
                PhoneNumber = phone,
                Address = address,
                PortalRedirect = _schoolSettings.GetPortalRoute(primaryRole),
                AssignedClassId = assignedClassId,
                AssignedClassName = assignedClassName
            };
        }

        public async Task<UserProfileResponse> UpdateUserProfile(string userId, UpdateUserProfileRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                user.DisplayName = request.DisplayName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                user.PhoneNumber = request.PhoneNumber.Trim();
            }

            await _userManager.UpdateAsync(user);

            var parentRepo = _unitOfWork.GetRepository<Parent>();
            var parent = await parentRepo.GetSingleByAsync(p => p.UserId == userId);
            if (parent != null)
            {
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                    parent.Phone = request.PhoneNumber.Trim();
                if (!string.IsNullOrWhiteSpace(request.Address))
                    parent.Address = request.Address.Trim();
                if (!string.IsNullOrWhiteSpace(request.DisplayName))
                    parent.FullName = request.DisplayName.Trim();
                await parentRepo.UpdateAsync(parent);
                await _unitOfWork.SaveChangesAsync();
            }

            var staffRepo = _unitOfWork.GetRepository<Staff>();
            var staff = await staffRepo.GetSingleByAsync(s => s.UserId == userId);
            if (staff != null)
            {
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                    staff.Phone = request.PhoneNumber.Trim();
                await staffRepo.UpdateAsync(staff);
                await _unitOfWork.SaveChangesAsync();
            }

            return await GetCurrentUserProfile(userId);
        }

        public async Task<bool> ChangePassword(string userId, ChangePasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var res = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!res.Succeeded)
            {
                throw new InvalidOperationException(string.Join("\n", res.Errors.Select(e => e.Description)));
            }
            return true;
        }

        public async Task<bool> ResetPassword(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found with email");
            }

            var res = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!res.Succeeded)
            {
                throw new InvalidOperationException(string.Join("\n", res.Errors.Select(e => e.Description)));
            }
            return true;
        }

        public async Task<bool> ConfirmEmail(ConfirmEmailRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email.Trim().ToLower());
            if (user == null)
            {
                throw new KeyNotFoundException("User not found with provided email address.");
            }

            var res = await _userManager.ConfirmEmailAsync(user, request.Token);
            if (!res.Succeeded)
            {
                throw new InvalidOperationException(string.Join("\n", res.Errors.Select(e => e.Description)));
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            return true;
        }

        public async Task<LoggedInUserResponse> RefreshAccessToken(RefreshAccessTokenRequest request)
        {
            var principal = _jwtAuthenticator.GetPrincipalFromExpiredToken(request.AccessToken);
            var userId = principal.FindFirst("Id")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("Invalid token payload");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.Active)
            {
                throw new InvalidOperationException("Invalid user");
            }

            var storedRefreshToken = await _userManager.GetAuthenticationTokenAsync(user, _schoolSettings.TokenProviderName, "RefreshToken");
            bool isRefreshTokenValid = await _userManager.VerifyUserTokenAsync(user, TokenProviders.RefreshTokenProvider, "RefreshToken", request.RefreshToken);
            if (!isRefreshTokenValid || storedRefreshToken != request.RefreshToken)
            {
                throw new InvalidOperationException("Invalid refresh token");
            }

            var newToken = await _jwtAuthenticator.GenerateJwtToken(user);
            var newRefreshToken = await _jwtAuthenticator.GenerateRefreshToken(user);

            var roles = await _userManager.GetRolesAsync(user);
            string primaryRole = roles.FirstOrDefault()
                ?? throw new InvalidOperationException("User account has no assigned role. Contact your administrator.");

            var (menuItems, permissions) = await GetUserNavigationAndPermissions(user.Id, roles);
            var (assignedClassId, assignedClassName) = await ResolveUserClassAssignmentAsync(user.Id);

            return new LoggedInUserResponse
            {
                Token = newToken.Token,
                RefreshToken = newRefreshToken,
                User = new UserProfileResponse
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    DisplayName = user.DisplayName,
                    Role = primaryRole,
                    AvatarUrl = user.AvatarUrl,
                    PhoneNumber = user.PhoneNumber,
                    PortalRedirect = _schoolSettings.GetPortalRoute(primaryRole),
                    AssignedClassId = assignedClassId,
                    AssignedClassName = assignedClassName
                },
                MenuItems = menuItems,
                Permissions = permissions
            };
        }
    }

    public class TermService : ITermService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TermService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TermResponse>> GetAllTerms()
        {
            var terms = await _unitOfWork.GetRepository<AcademicTerm>().GetAllAsync(q => q.OrderByDescending(t => t.StartDate));
            return _mapper.Map<IEnumerable<TermResponse>>(terms);
        }

        public async Task<TermResponse?> GetActiveTerm()
        {
            var term = await _unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => t.IsActive && t.Active);
            return term != null ? _mapper.Map<TermResponse>(term) : null;
        }

        public async Task<TermResponse> CreateTerm(CreateTermRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            string normalizedSession = request.SessionName.Trim();
            var termRepo = _unitOfWork.GetRepository<AcademicTerm>();

            var existing = await termRepo.GetSingleByAsync(
                t => t.SessionName.ToLower() == normalizedSession.ToLower() && t.TermNumber == request.TermNumber);

            if (existing != null)
            {
                throw new InvalidOperationException($"Term {request.TermNumber} for academic session '{normalizedSession}' already exists.");
            }

            if (request.IsActive)
            {
                var otherTerms = await termRepo.GetByAsync(t => t.IsActive);
                foreach (var other in otherTerms)
                {
                    other.IsActive = false;
                    other.UpdatedAt = DateTime.UtcNow;
                    await termRepo.UpdateAsync(other);
                }
            }

            var newTerm = new AcademicTerm
            {
                Id = Guid.NewGuid(),
                SessionName = normalizedSession,
                TermNumber = request.TermNumber,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = request.IsActive,
                Cat1Deadline = request.Cat1Deadline,
                Cat2Deadline = request.Cat2Deadline,
                ExamDeadline = request.ExamDeadline,
                Active = true,
                CreatedAt = DateTime.UtcNow
            };

            await termRepo.AddAsync(newTerm);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<TermResponse>(newTerm);
        }

        public async Task<TermResponse> UpdateTerm(Guid id, UpdateTermRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            string normalizedSession = request.SessionName.Trim();
            var termRepo = _unitOfWork.GetRepository<AcademicTerm>();
            var term = await termRepo.GetByIdAsync(id);
            if (term == null)
            {
                throw new KeyNotFoundException("Academic term not found");
            }

            var collision = await termRepo.GetSingleByAsync(
                t => t.Id != id && t.SessionName.ToLower() == normalizedSession.ToLower() && t.TermNumber == request.TermNumber);

            if (collision != null)
            {
                throw new InvalidOperationException($"Term {request.TermNumber} for academic session '{normalizedSession}' already exists.");
            }

            if (request.IsActive)
            {
                var otherTerms = await termRepo.GetByAsync(t => t.Id != id && t.IsActive);
                foreach (var other in otherTerms)
                {
                    other.IsActive = false;
                    other.UpdatedAt = DateTime.UtcNow;
                    await termRepo.UpdateAsync(other);
                }
            }

            term.SessionName = normalizedSession;
            term.TermNumber = request.TermNumber;
            term.StartDate = request.StartDate;
            term.EndDate = request.EndDate;
            term.IsActive = request.IsActive;
            term.UpdatedAt = DateTime.UtcNow;

            await termRepo.UpdateAsync(term);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<TermResponse>(term);
        }

        public async Task<DeadlinesResponse> ConfigureDeadlines(Guid id, ConfigureDeadlinesRequest request)
        {
            var termRepo = _unitOfWork.GetRepository<AcademicTerm>();
            var term = await termRepo.GetByIdAsync(id);
            if (term == null)
            {
                throw new KeyNotFoundException("Academic term not found");
            }

            term.Cat1Deadline = request.Cat1Deadline;
            term.Cat2Deadline = request.Cat2Deadline;
            term.ExamDeadline = request.ExamDeadline;
            term.UpdatedAt = DateTime.UtcNow;

            await termRepo.UpdateAsync(term);
            await _unitOfWork.SaveChangesAsync();

            return new DeadlinesResponse
            {
                Success = true,
                TermId = term.Id,
                Deadlines = new DeadlinesDto
                {
                    Cat1Deadline = term.Cat1Deadline,
                    Cat2Deadline = term.Cat2Deadline,
                    ExamDeadline = term.ExamDeadline
                }
            };
        }
    }
}
