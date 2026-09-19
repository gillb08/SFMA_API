using AutoMapper;
using Microsoft.AspNetCore.Identity;
using SFMA_API.Data.Interfaces;
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

        public AuthenticationService(UserManager<ApplicationUser> userManager, IJWTAuthenticator jwtAuthenticator, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _jwtAuthenticator = jwtAuthenticator;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
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
            string primaryRole = roles.FirstOrDefault() ?? "student";

            var token = await _jwtAuthenticator.GenerateJwtToken(user);
            var refreshToken = await _jwtAuthenticator.GenerateRefreshToken(user);

            string portalRedirect = primaryRole switch
            {
                "super_admin" => "/portal/admin",
                "academic_admin" => "/portal/academic-admin",
                "financial_admin" => "/portal/bursary",
                "academic_head" => "/portal/academic-head",
                "financial_head" => "/portal/financial-head",
                "teacher" => "/portal/teacher",
                "parent" => "/portal/parent",
                "student" => "/portal/student",
                _ => "/portal"
            };

            var userProfile = new UserProfileResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                Role = primaryRole,
                AvatarUrl = user.AvatarUrl,
                PortalRedirect = portalRedirect
            };

            return new LoggedInUserResponse
            {
                Token = token.Token,
                RefreshToken = refreshToken,
                User = userProfile
            };
        }

        public async Task<bool> Logout(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.RemoveAuthenticationTokenAsync(user, "SFMA", "RefreshToken");
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
            string primaryRole = roles.FirstOrDefault() ?? "student";

            return new UserProfileResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                Role = primaryRole,
                AvatarUrl = user.AvatarUrl,
                PortalRedirect = $"/portal/{primaryRole.Replace("_", "-")}"
            };
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

            bool isRefreshTokenValid = await _userManager.VerifyUserTokenAsync(user, TokenProviders.RefreshTokenProvider, "RefreshToken", request.RefreshToken);
            if (!isRefreshTokenValid)
            {
                throw new InvalidOperationException("Invalid refresh token");
            }

            var newToken = await _jwtAuthenticator.GenerateJwtToken(user);
            var newRefreshToken = await _jwtAuthenticator.GenerateRefreshToken(user);

            var roles = await _userManager.GetRolesAsync(user);
            string primaryRole = roles.FirstOrDefault() ?? "student";

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
                    PortalRedirect = $"/portal/{primaryRole.Replace("_", "-")}"
                }
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

        public async Task<TermResponse> UpdateTerm(Guid id, UpdateTermRequest request)
        {
            var termRepo = _unitOfWork.GetRepository<AcademicTerm>();
            var term = await termRepo.GetByIdAsync(id);
            if (term == null)
            {
                throw new KeyNotFoundException("Academic term not found");
            }

            if (request.IsActive)
            {
                var otherTerms = await termRepo.GetByAsync(t => t.Id != id && t.IsActive);
                foreach (var other in otherTerms)
                {
                    other.IsActive = false;
                    await termRepo.UpdateAsync(other);
                }
            }

            term.SessionName = request.SessionName;
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
