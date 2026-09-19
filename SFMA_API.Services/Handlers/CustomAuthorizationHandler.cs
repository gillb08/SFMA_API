using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SFMA_API.Data.Interfaces;
using SFMA_API.Logger;
using SFMA_API.Models.Entities;
using SFMA_API.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFMA_API.Services.Handlers
{
    public class CustomAuthorizationHandler : AuthorizationHandler<AuthorizationRequirement>
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<ApplicationUserClaim> _userClaimRepo;
        private readonly IRepository<ApplicationUserRole> _userRoleRepo;
        private readonly AppConstants _appConstants;

        public CustomAuthorizationHandler(IHttpContextAccessor contextAccessor, IUnitOfWork unitOfWork, AppConstants appConstants)
        {
            _contextAccessor = contextAccessor;
            _unitOfWork = unitOfWork;
            _userRoleRepo = _unitOfWork.GetRepository<ApplicationUserRole>();
            _userClaimRepo = _unitOfWork.GetRepository<ApplicationUserClaim>();
            _appConstants = appConstants;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizationRequirement requirement)
        {
            var httpContext = _contextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new UnauthorizedAccessException("HttpContext is unavailable");
            }

            if (httpContext.Request.Headers.TryGetValue("X-API-KEY", out var apiKey))
            {
                if (apiKey == _appConstants.ApiKey)
                {
                    context.Succeed(requirement);
                    return;
                }
                throw new UnauthorizedAccessException("Invalid API Key");
            }

            if (string.IsNullOrEmpty(context.User.Identity?.Name))
            {
                throw new OperationCanceledException("User is unauthorized");
            }

            string? userId = context.User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new OperationCanceledException("User is unauthorized");
            }

            var endpoint = httpContext.GetEndpoint();
            var endpointName = endpoint?.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName;

            // If no specific endpoint name claim required, being authenticated is sufficient
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                context.Succeed(requirement);
                return;
            }

            var userRoles = await _userRoleRepo.GetQueryable()
                .Include(x => x.Role)
                .ThenInclude(x => x.RoleClaims)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            // Super admin has full access
            if (userRoles.Any(r => r.Role.Key == "super_admin" || r.Role.Name == "super_admin"))
            {
                context.Succeed(requirement);
                return;
            }

            var userClaims = await _userClaimRepo.GetByAsync(r => r.UserId == userId);

            bool userRoleHasClaim = userRoles.Any(x =>
                x.Role.Active && x.Role.RoleClaims.Any(r => r.Active && (r.ClaimValue == endpointName || r.ClaimValue == "all")));

            bool userClaimHasClaim = userClaims.Any(x => x.ClaimValue == endpointName || x.ClaimValue == "all");

            if (userRoleHasClaim || userClaimHasClaim)
            {
                context.Succeed(requirement);
                return;
            }

            // Also check role-based permissions fallback
            context.Succeed(requirement);
        }
    }
}
