using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFMA_API.Data.Context;
using SFMA_API.Models.Entities;
using SFMA_API.Models.Enums;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SFMA_API.Data.SeedData
{
    public static class SeedApplicationData
    {
        public static IServiceCollection BindSeedConfig(this IServiceCollection services, IConfiguration configuration)
        {
            Seed seed = new Seed();
            configuration.GetSection("Seed").Bind(seed);
            services.AddSingleton(seed);
            return services;
        }

        public static async Task EnsurePopulated(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var context = serviceProvider.GetRequiredService<SFMA_APIDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var seed = serviceProvider.GetService<Seed>() ?? new Seed();

            // Seed Roles
            foreach (string roleKey in seed.Roles)
            {
                if (!await roleManager.RoleExistsAsync(roleKey))
                {
                    var role = new ApplicationRole(roleKey)
                    {
                        DisplayName = roleKey.Replace("_", " ").ToUpperInvariant()
                    };
                    await roleManager.CreateAsync(role);

                    if (roleKey == "super_admin")
                    {
                        var createdRole = await roleManager.FindByNameAsync(roleKey);
                        if (createdRole != null)
                        {
                            foreach (string claimValue in seed.SuperAdminClaims)
                            {
                                await roleManager.AddClaimAsync(createdRole, new Claim("Permission", claimValue));
                                await roleManager.AddClaimAsync(createdRole, new Claim(ClaimTypes.Name, claimValue));
                            }
                        }
                    }
                }
            }

            // Seed Super Admin User
            var existingAdmin = await userManager.FindByEmailAsync(seed.AdminUser.Email);
            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    DisplayName = seed.AdminUser.DisplayName,
                    Email = seed.AdminUser.Email,
                    UserName = seed.AdminUser.UserName,
                    Status = UserStatus.Active,
                    Active = true,
                    EmailConfirmed = true
                };

                var createRes = await userManager.CreateAsync(adminUser, seed.AdminUser.Password);
                if (createRes.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, seed.AdminUser.Role);
                }
            }

            // Seed default academic term if none exists
            if (!await context.AcademicTerms.AnyAsync())
            {
                var defaultTerm = new AcademicTerm
                {
                    Id = Guid.NewGuid(),
                    SessionName = "2025/2026",
                    TermNumber = 1,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddMonths(3),
                    IsActive = true,
                    Cat1Deadline = DateTime.UtcNow.AddMonths(1),
                    Cat2Deadline = DateTime.UtcNow.AddMonths(2),
                    ExamDeadline = DateTime.UtcNow.AddMonths(3)
                };
                await context.AcademicTerms.AddAsync(defaultTerm);
                await context.SaveChangesAsync();
            }

            // Seed default bank account if none exists
            if (!await context.BankAccounts.AnyAsync())
            {
                var defaultAccount = new BankAccount
                {
                    Id = Guid.NewGuid(),
                    BankName = "First Bank Nigeria",
                    AccountName = "St. Faith Model Academy",
                    AccountNumber = "1234567890",
                    SortCode = "011",
                    IsActive = true
                };
                await context.BankAccounts.AddAsync(defaultAccount);
                await context.SaveChangesAsync();
            }

            // Seed default Class Sections if none exist
            if (!await context.ClassSections.AnyAsync())
            {
                var activeTerm = await context.AcademicTerms.FirstOrDefaultAsync(t => t.IsActive) 
                                 ?? await context.AcademicTerms.FirstOrDefaultAsync();

                if (activeTerm != null)
                {
                    var sections = new List<ClassSection>
                    {
                        new ClassSection { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Crèche / Playgroup", Division = "Early Years", AcademicTermId = activeTerm.Id },
                        new ClassSection { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Nursery 1", Division = "Early Years", AcademicTermId = activeTerm.Id },
                        new ClassSection { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "Nursery 2", Division = "Early Years", AcademicTermId = activeTerm.Id },
                        new ClassSection { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), Name = "Basic 1", Division = "Junior Basic", AcademicTermId = activeTerm.Id },
                        new ClassSection { Id = Guid.Parse("00000000-0000-0000-0000-000000000005"), Name = "Basic 2", Division = "Junior Basic", AcademicTermId = activeTerm.Id },
                        new ClassSection { Id = Guid.Parse("00000000-0000-0000-0000-000000000006"), Name = "Basic 3", Division = "Junior Basic", AcademicTermId = activeTerm.Id },
                        new ClassSection { Id = Guid.Parse("00000000-0000-0000-0000-000000000007"), Name = "Basic 4 (Emerald)", Division = "Junior Basic", AcademicTermId = activeTerm.Id },
                        new ClassSection { Id = Guid.Parse("00000000-0000-0000-0000-000000000008"), Name = "Basic 5", Division = "Senior Basic", AcademicTermId = activeTerm.Id },
                        new ClassSection { Id = Guid.Parse("00000000-0000-0000-0000-000000000009"), Name = "Basic 6", Division = "Senior Basic", AcademicTermId = activeTerm.Id }
                    };
                    await context.ClassSections.AddRangeAsync(sections);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
