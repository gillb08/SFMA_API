using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SFMA_API.Data.Interfaces;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using SFMA_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SFMA_API.Services.Implementation
{
    public class MenuService : IMenuService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MenuService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MenuResponse>> GetAllMenus()
        {
            var menuRepo = _unitOfWork.GetRepository<Menu>();
            var menus = await menuRepo.GetQueryable()
                .OrderBy(m => m.Order)
                .ToListAsync();

            return _mapper.Map<IEnumerable<MenuResponse>>(menus);
        }

        public async Task<MenuResponse> CreateMenu(CreateMenuRequest request)
        {
            var menuRepo = _unitOfWork.GetRepository<Menu>();
            var menu = new Menu
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Icon = request.Icon,
                Route = request.Route,
                Order = request.Order,
                Active = true,
                ClaimsJson = request.Claims != null && request.Claims.Any()
                    ? JsonSerializer.Serialize(request.Claims.Select(c => c.Trim().ToLowerInvariant()).Distinct())
                    : null
            };

            await menuRepo.AddAsync(menu);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<MenuResponse>(menu);
        }

        public async Task<MenuResponse> UpdateMenu(Guid id, UpdateMenuRequest request)
        {
            var menuRepo = _unitOfWork.GetRepository<Menu>();
            var menu = await menuRepo.GetSingleByAsync(m => m.Id == id);
            if (menu == null)
            {
                throw new KeyNotFoundException($"Menu with ID '{id}' was not found.");
            }

            menu.Title = request.Title;
            menu.Icon = request.Icon;
            menu.Route = request.Route;
            menu.Order = request.Order;
            menu.Active = request.Active;
            menu.UpdatedAt = DateTime.UtcNow;

            if (request.Claims != null)
            {
                menu.ClaimsJson = request.Claims.Any()
                    ? JsonSerializer.Serialize(request.Claims.Select(c => c.Trim().ToLowerInvariant()).Distinct())
                    : null;
            }

            await menuRepo.UpdateAsync(menu);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<MenuResponse>(menu);
        }

        public async Task<bool> AddClaimsToMenu(AddClaimsToMenuRequest request)
        {
            var menuRepo = _unitOfWork.GetRepository<Menu>();
            var menu = await menuRepo.GetSingleByAsync(m => m.Id == request.MenuId);
            if (menu == null)
            {
                throw new KeyNotFoundException($"Menu with ID '{request.MenuId}' was not found.");
            }

            var currentClaims = !string.IsNullOrEmpty(menu.ClaimsJson)
                ? JsonSerializer.Deserialize<List<string>>(menu.ClaimsJson) ?? new List<string>()
                : new List<string>();

            var mergedClaims = currentClaims
                .Concat(request.Claims)
                .Select(c => c.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            menu.ClaimsJson = JsonSerializer.Serialize(mergedClaims);
            menu.UpdatedAt = DateTime.UtcNow;

            await menuRepo.UpdateAsync(menu);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<string>> GetMenuItems(IEnumerable<string> claims)
        {
            var menuRepo = _unitOfWork.GetRepository<Menu>();
            var menus = await menuRepo.GetQueryable()
                .Where(m => m.Active)
                .OrderBy(m => m.Order)
                .ToListAsync();

            var claimSet = new HashSet<string>(claims.Select(c => c.Trim().ToLowerInvariant()), StringComparer.OrdinalIgnoreCase);
            bool isSuperAdmin = claimSet.Contains("all");

            var accessibleMenuTitles = new List<string>();

            foreach (var menu in menus)
            {
                if (string.IsNullOrWhiteSpace(menu.ClaimsJson))
                {
                    accessibleMenuTitles.Add(menu.Title);
                    continue;
                }

                if (isSuperAdmin)
                {
                    accessibleMenuTitles.Add(menu.Title);
                    continue;
                }

                try
                {
                    var menuClaims = JsonSerializer.Deserialize<List<string>>(menu.ClaimsJson);
                    if (menuClaims == null || !menuClaims.Any() || menuClaims.Any(mc => claimSet.Contains(mc)))
                    {
                        accessibleMenuTitles.Add(menu.Title);
                    }
                }
                catch
                {
                    accessibleMenuTitles.Add(menu.Title);
                }
            }

            return accessibleMenuTitles;
        }
    }
}
