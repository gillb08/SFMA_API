using System.Collections.Generic;

namespace SFMA_API.Data.SeedData
{
    public class Seed
    {
        public List<string> Roles { get; set; } = new List<string>
        {
            "super_admin",
            "academic_admin",
            "financial_admin",
            "academic_head",
            "financial_head",
            "teacher",
            "parent",
            "student"
        };

        public List<string> SuperAdminClaims { get; set; } = new List<string>
        {
            "staff-crud",
            "staff-create-accounts",
            "staff-suspend-reactivate",
            "staff-reassign-role",
            "staff-reset-password",
            "rbac-manage",
            "students-admit",
            "students-view-all",
            "students-edit-biodata",
            "idcards-generate-print",
            "attendance-supervisor-unlock",
            "attendance-view-audit",
            "attendance-view",
            "assessments-configure-deadlines",
            "assessments-view",
            "results-download",
            "fees-view-ledger",
            "requisitions-submit",
            "requisitions-approve-final",
            "requisitions-query",
            "timetable-view",
            "assignments-view",
            "inquiries-respond",
            "dashboard-executive",
            "dashboard-financial"
        };

        public AdminUserSeed AdminUser { get; set; } = new AdminUserSeed
        {
            DisplayName = "Super Administrator",
            Email = "admin@stfaithacademy.edu.ng",
            UserName = "admin@stfaithacademy.edu.ng",
            Role = "super_admin",
            Password = "Password@123!"
        };
    }

    public class AdminUserSeed
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
