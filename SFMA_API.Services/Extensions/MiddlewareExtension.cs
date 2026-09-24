using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SFMA_API.Data.Context;
using SFMA_API.Data.Implementation;
using SFMA_API.Data.Interfaces;
using SFMA_API.Services.Handlers;
using SFMA_API.Services.Implementation;
using SFMA_API.Services.Infrastructure;
using SFMA_API.Services.Interfaces;

namespace SFMA_API.Services.Extensions
{
    public static class MiddlewareExtension
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.AddSingleton<IPermissionCacheService, PermissionCacheService>();
            services.AddTransient<IJWTAuthenticator, JWTAuthenticator>();
            services.AddTransient<IAuthorizationHandler, CustomAuthorizationHandler>();
            services.AddTransient<IUnitOfWork, UnitOfWork<SFMA_APIDbContext>>();
            services.AddTransient<IServiceFactory, ServiceFactory>();
            services.AddTransient<IAuthenticationService, AuthenticationService>();
            services.AddTransient<ITermService, TermService>();
            services.AddTransient<IStaffService, StaffService>();
            services.AddTransient<IRolePermissionService, RoleClaimService>();
            services.AddTransient<IRoleClaimService, RoleClaimService>();
            services.AddTransient<IMenuService, MenuService>();
            services.AddTransient<IStudentService, StudentService>();
            services.AddTransient<IAttendanceService, AttendanceService>();
            services.AddTransient<IAssessmentService, AssessmentService>();
            services.AddTransient<IFeeService, FeeService>();
            services.AddTransient<IRequisitionService, RequisitionService>();
            services.AddTransient<ISchemeAndTimetableService, SchemeAndTimetableService>();
            services.AddTransient<ILessonNoteAndAssignmentService, LessonNoteAndAssignmentService>();
            services.AddTransient<IInquiryAndRequirementService, InquiryAndRequirementService>();
            services.AddTransient<IDashboardService, DashboardService>();

            // Notification System & External Gateways
            services.AddScoped<ISchoolNotificationService, SchoolNotificationService>();
            services.AddHttpClient<IEmailSender, ResendEmailSender>((sp, client) =>
            {
                var emailOpts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SFMA_API.Models.Configuration.EmailOptions>>().Value;
                string baseUrl = emailOpts.Resend?.BaseUrl ?? "https://api.resend.com";
                client.BaseAddress = new System.Uri(baseUrl.TrimEnd('/') + '/');
            });
            services.AddHttpClient<ISmsSender, SmsSender>((sp, client) =>
            {
                var smsOpts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SFMA_API.Models.Configuration.SmsOptions>>().Value;
                string baseUrl = smsOpts.BaseUrl ?? "https://api.ng.termii.com";
                client.BaseAddress = new System.Uri(baseUrl.TrimEnd('/') + '/');
            });
        }
    }
}
