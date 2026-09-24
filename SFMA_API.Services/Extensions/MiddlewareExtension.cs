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
        }
    }
}
