using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SFMA_API.Data.Interfaces;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using SFMA_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SFMA_API.Services.Implementation
{
    public class RoleClaimService : IRoleClaimService, IRolePermissionService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly EndpointDataSource _endpointDataSource;
        private readonly IPermissionCacheService _cacheService;

        public RoleClaimService(
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            EndpointDataSource endpointDataSource,
            IPermissionCacheService cacheService)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _endpointDataSource = endpointDataSource;
            _cacheService = cacheService;
        }

        public Task<IEnumerable<string>> GetRouteNames()
        {
            var routeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var endpoint in _endpointDataSource.Endpoints)
            {
                var endpointName = endpoint.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName;
                if (!string.IsNullOrWhiteSpace(endpointName))
                {
                    routeNames.Add(endpointName.Trim().ToLowerInvariant());
                }
            }

            return Task.FromResult<IEnumerable<string>>(routeNames.OrderBy(x => x).ToList());
        }

        public async Task<IEnumerable<RoleResponse>> GetAllRoles()
        {
            var roles = await _roleManager.Roles
                .Include(r => r.RoleClaims)
                .OrderBy(r => r.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<RoleResponse>>(roles);
        }

        public async Task<RoleResponse?> GetRolePermissions(string roleKey)
        {
            var role = await _roleManager.Roles
                .Include(r => r.RoleClaims)
                .FirstOrDefaultAsync(r => r.Key == roleKey || r.Name == roleKey || r.Id == roleKey);

            return role != null ? _mapper.Map<RoleResponse>(role) : null;
        }

        public async Task<bool> UpdateRoleClaims(UpdateRoleClaimsRequest request)
        {
            var role = await _roleManager.Roles
                .Include(r => r.RoleClaims)
                .FirstOrDefaultAsync(r => r.Id == request.RoleId || r.Key == request.RoleKey || r.Name == request.RoleKey);

            if (role == null)
            {
                throw new KeyNotFoundException($"Role '{request.RoleId ?? request.RoleKey}' was not found.");
            }

            var roleClaimRepo = _unitOfWork.GetRepository<ApplicationRoleClaim>();

            // Remove existing role claims for this role
            var existingClaims = await roleClaimRepo.GetByAsync(rc => rc.RoleId == role.Id);
            foreach (var existing in existingClaims)
            {
                await roleClaimRepo.DeleteAsync(existing);
            }

            // Add requested claims
            var distinctClaims = request.Claims
                .Select(c => c.Trim().ToLowerInvariant())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct();

            foreach (var claimVal in distinctClaims)
            {
                var newClaim = new ApplicationRoleClaim
                {
                    RoleId = role.Id,
                    ClaimType = "permission",
                    ClaimValue = claimVal,
                    Active = request.Active
                };
                await roleClaimRepo.AddAsync(newClaim);
            }

            role.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            // Invalidate all cached user permissions
            _cacheService.InvalidateAll();

            return true;
        }

        public async Task<bool> UpdateUserClaims(UpdateUserClaimsRequest request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID '{request.UserId}' was not found.");
            }

            var userClaimRepo = _unitOfWork.GetRepository<ApplicationUserClaim>();

            // Remove existing direct user claims
            var existingClaims = await userClaimRepo.GetByAsync(uc => uc.UserId == user.Id);
            foreach (var existing in existingClaims)
            {
                await userClaimRepo.DeleteAsync(existing);
            }

            // Add new claims
            var distinctClaims = request.Claims
                .Select(c => c.Trim().ToLowerInvariant())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct();

            foreach (var claimVal in distinctClaims)
            {
                var newClaim = new ApplicationUserClaim
                {
                    UserId = user.Id,
                    ClaimType = "permission",
                    ClaimValue = claimVal,
                    Active = request.Active
                };
                await userClaimRepo.AddAsync(newClaim);
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Invalidate cached user permissions for this specific user
            _cacheService.InvalidateUserPermissions(user.Id);

            return true;
        }
    }
}
