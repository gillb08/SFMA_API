using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SFMA_API.Data.Interfaces;
using SFMA_API.Logger;
using SFMA_API.Models.Entities;
using SFMA_API.Services.Infrastructure;
using SFMA_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SFMA_API.Services.Handlers
{
    public class CustomAuthorizationHandler : AuthorizationHandler<AuthorizationRequirement>
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppConstants _appConstants;
        private readonly IPermissionCacheService _cacheService;

        public CustomAuthorizationHandler(
            IHttpContextAccessor contextAccessor,
            IUnitOfWork unitOfWork,
            AppConstants appConstants,
            IPermissionCacheService cacheService)
        {
            _contextAccessor = contextAccessor;
            _unitOfWork = unitOfWork;
            _appConstants = appConstants;
            _cacheService = cacheService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizationRequirement requirement)
        {
            var httpContext = _contextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new UnauthorizedAccessException("HttpContext is unavailable.");
            }

            // 1. API Key Bypass check
            if (httpContext.Request.Headers.TryGetValue("X-API-KEY", out var apiKey))
            {
                if (apiKey == _appConstants.ApiKey)
                {
                    context.Succeed(requirement);
                    return;
                }
                throw new UnauthorizedAccessException("Invalid API Key");
            }

            // 2. User Authentication Validation
            if (context.User.Identity == null || !context.User.Identity.IsAuthenticated || string.IsNullOrEmpty(context.User.Identity.Name))
            {
                throw new OperationCanceledException("User is unauthorized");
            }

            string? userId = context.User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new OperationCanceledException("User is unauthorized");
            }

            // 3. Extract Endpoint Route Name
            var endpoint = httpContext.GetEndpoint();
            var endpointName = endpoint?.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName;

            // If no specific endpoint name claim required, authenticated access is sufficient
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                context.Succeed(requirement);
                return;
            }

            var normalizedEndpoint = endpointName.Trim().ToLowerInvariant();

            // 4. Retrieve cached permissions for user
            var userPermissions = await _cacheService.GetUserPermissionsAsync(userId, async () =>
            {
                var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var userRoleRepo = _unitOfWork.GetRepository<ApplicationUserRole>();
                var userClaimRepo = _unitOfWork.GetRepository<ApplicationUserClaim>();

                var userRoles = await userRoleRepo.GetQueryable(
                    include: q => q.Include(x => x.Role).ThenInclude(r => r.RoleClaims))
                    .Where(r => r.UserId == userId)
                    .ToListAsync();

                // Unrestricted access for super admin
                if (userRoles.Any(r => r.Role.Active && (r.Role.Key == "super_admin" || r.Role.Name == "super_admin" || r.Role.Key == "superadmin")))
                {
                    permissions.Add("all");
                    return permissions;
                }

                // Add active role claims
                foreach (var userRole in userRoles)
                {
                    if (userRole.Role.Active)
                    {
                        foreach (var roleClaim in userRole.Role.RoleClaims)
                        {
                            if (roleClaim.Active && !string.IsNullOrWhiteSpace(roleClaim.ClaimValue))
                            {
                                permissions.Add(roleClaim.ClaimValue.Trim().ToLowerInvariant());
                            }
                        }
                    }
                }

                // Add active direct user claims
                var directClaims = await userClaimRepo.GetByAsync(r => r.UserId == userId && r.Active);
                foreach (var claim in directClaims)
                {
                    if (!string.IsNullOrWhiteSpace(claim.ClaimValue))
                    {
                        permissions.Add(claim.ClaimValue.Trim().ToLowerInvariant());
                    }
                }

                return permissions;
            });

            // 5. Check if user possesses required route permission or super admin token
            if (userPermissions.Contains("all") || userPermissions.Contains(normalizedEndpoint))
            {
                context.Succeed(requirement);
                return;
            }

            // Deny access with 403
            throw new UnauthorizedAccessException($"User is unauthorized to access '{endpointName}'.");
        }
    }
}
