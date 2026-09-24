using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFMA_API.Data.Context;
using SFMA_API.Models.Entities;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
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

            // 0. Automatically apply any pending EF Core database migrations
            await context.Database.MigrateAsync();

            // 1. Seed Roles & Role Claims
            foreach (string roleKey in seed.Roles)
            {
                var role = await roleManager.FindByNameAsync(roleKey);
                if (role == null)
                {
                    role = new ApplicationRole(roleKey)
                    {
                        DisplayName = roleKey.Replace("_", " ").ToUpperInvariant()
                    };
                    await roleManager.CreateAsync(role);
                }

                if (seed.DefaultRoleClaims.TryGetValue(roleKey, out var claims))
                {
                    var existingClaims = await roleManager.GetClaimsAsync(role);
                    var existingClaimValues = existingClaims.Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (string claimValue in claims)
                    {
                        if (!existingClaimValues.Contains(claimValue))
                        {
                            await roleManager.AddClaimAsync(role, new Claim("permission", claimValue));
                            await roleManager.AddClaimAsync(role, new Claim(ClaimTypes.Name, claimValue));
                        }
                    }
                }
            }

            // 2. Seed Super Admin User
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

            // 3. Seed default Menus if none exist
            if (!await context.Menus.AnyAsync())
            {
                var defaultMenus = new List<Menu>
                {
                    new Menu
                    {
                        Id = Guid.NewGuid(),
                        Title = "Dashboard",
                        Icon = "dashboard",
                        Route = "/dashboard",
                        Order = 1,
                        Active = true,
                        ClaimsJson = JsonSerializer.Serialize(new List<string> { "get-executive-dashboard", "get-academic-snapshot", "get-financial-dashboard", "get-budget-progress" })
                    },
                    new Menu
                    {
                        Id = Guid.NewGuid(),
                        Title = "Staff Management",
                        Icon = "people",
                        Route = "/staff",
                        Order = 2,
                        Active = true,
                        ClaimsJson = JsonSerializer.Serialize(new List<string> { "get-all-staff", "get-staff-by-id", "create-staff", "update-staff" })
                    },
                    new Menu
                    {
                        Id = Guid.NewGuid(),
                        Title = "Student Management",
                        Icon = "school",
                        Route = "/students",
                        Order = 3,
                        Active = true,
                        ClaimsJson = JsonSerializer.Serialize(new List<string> { "get-all-students", "get-student-by-id", "admit-student", "update-student-biodata" })
                    },
                    new Menu
                    {
                        Id = Guid.NewGuid(),
                        Title = "Attendance",
                        Icon = "fact_check",
                        Route = "/attendance",
                        Order = 4,
                        Active = true,
                        ClaimsJson = JsonSerializer.Serialize(new List<string> { "get-daily-attendance", "get-student-monthly-attendance", "batch-mark-attendance" })
                    },
                    new Menu
                    {
                        Id = Guid.NewGuid(),
                        Title = "Assessments & Results",
                        Icon = "grade",
                        Route = "/assessments",
                        Order = 5,
                        Active = true,
                        ClaimsJson = JsonSerializer.Serialize(new List<string> { "get-assessment-broadsheet", "batch-update-scores", "submit-broadsheet", "get-student-results" })
                    },
                    new Menu
                    {
                        Id = Guid.NewGuid(),
                        Title = "Fee Management",
                        Icon = "payments",
                        Route = "/fees",
                        Order = 6,
                        Active = true,
                        ClaimsJson = JsonSerializer.Serialize(new List<string> { "get-fee-schedule", "get-fee-ledger", "get-student-fee-summary", "post-fee-teller", "get-student-receipts" })
                    },
                    new Menu
                    {
                        Id = Guid.NewGuid(),
                        Title = "Requisitions & Budget",
                        Icon = "request_quote",
                        Route = "/requisitions",
                        Order = 7,
                        Active = true,
                        ClaimsJson = JsonSerializer.Serialize(new List<string> { "get-all-requisitions", "get-requisition-by-id", "create-requisition", "approve-requisition" })
                    },
                    new Menu
                    {
                        Id = Guid.NewGuid(),
                        Title = "Curriculum & Timetable",
                        Icon = "menu_book",
                        Route = "/academics",
                        Order = 8,
                        Active = true,
                        ClaimsJson = JsonSerializer.Serialize(new List<string> { "get-scheme-of-work", "get-timetable", "get-all-lesson-notes" })
                    },
                    new Menu
                    {
                        Id = Guid.NewGuid(),
                        Title = "Assignments",
                        Icon = "assignment",
                        Route = "/assignments",
                        Order = 9,
                        Active = true,
                        ClaimsJson = JsonSerializer.Serialize(new List<string> { "get-assignments", "create-assignment", "get-assignment-submissions", "submit-assignment" })
                    },
                    new Menu
                    {
                        Id = Guid.NewGuid(),
                        Title = "Helpdesk & Inquiries",
                        Icon = "support_agent",
                        Route = "/inquiries",
                        Order = 10,
                        Active = true,
                        ClaimsJson = JsonSerializer.Serialize(new List<string> { "get-inquiries", "get-inquiry-by-id", "create-inquiry", "add-inquiry-message" })
                    },
                    new Menu
                    {
                        Id = Guid.NewGuid(),
                        Title = "Role & Permissions",
                        Icon = "admin_panel_settings",
                        Route = "/roles",
                        Order = 11,
                        Active = true,
                        ClaimsJson = JsonSerializer.Serialize(new List<string> { "get-all-roles", "get-all-permissions", "get-all-role-claims", "update-role-claims", "get-all-menus" })
                    }
                };

                await context.Menus.AddRangeAsync(defaultMenus);
                await context.SaveChangesAsync();
            }

            // 4. Seed default academic term if none exists
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

            // 5. Seed default bank account if none exists
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

            // 6. Seed default Class Sections if none exist
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
