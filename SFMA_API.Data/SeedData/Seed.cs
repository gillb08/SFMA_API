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
            "all"
        };

        public Dictionary<string, List<string>> DefaultRoleClaims { get; set; } = new Dictionary<string, List<string>>
        {
            ["super_admin"] = new List<string> { "all" },
            ["academic_admin"] = new List<string>
            {
                "logout", "get-current-user-profile", "change-password",
                "get-all-terms", "get-active-term", "update-term", "configure-deadlines",
                "get-all-staff", "get-staff-by-id", "create-staff", "update-staff", "update-staff-status",
                "get-all-roles", "get-role-permissions", "get-all-permissions", "get-all-role-claims", "get-role-claims-by-key", "update-role-claims", "update-user-claims",
                "get-all-menus", "create-menu", "update-menu", "add-claims-to-menu",
                "get-all-students", "get-student-by-id", "admit-student", "update-student-biodata", "get-student-id-card", "batch-print-id-cards",
                "get-daily-attendance", "get-student-monthly-attendance", "batch-mark-attendance", "lock-attendance-register", "unlock-attendance-register", "get-attendance-audit-logs", "export-attendance-csv",
                "get-assessment-broadsheet", "batch-update-scores", "submit-broadsheet", "approve-broadsheet", "get-student-results", "export-broadsheet-results", "import-scores-csv", "get-score-template-csv",
                "get-fee-schedule", "get-fee-ledger", "get-student-fee-summary", "get-student-receipts", "get-receipt-pdf", "get-bank-accounts", "get-student-statement",
                "get-all-requisitions", "get-requisition-by-id", "create-requisition", "approve-requisition", "query-requisition",
                "get-scheme-of-work", "create-scheme-of-work", "update-scheme-of-work", "vet-scheme-of-work",
                "get-timetable", "update-timetable",
                "get-all-lesson-notes", "create-lesson-note", "review-lesson-note",
                "get-assignments", "create-assignment", "update-assignment", "get-assignment-submissions",
                "get-inquiries", "get-inquiry-by-id", "create-inquiry", "add-inquiry-message", "update-inquiry-status",
                "get-school-requirements", "toggle-requirement-status", "export-requirements-pdf",
                "get-executive-dashboard", "get-academic-snapshot", "get-budget-progress", "get-financial-dashboard"
            },
            ["teacher"] = new List<string>
            {
                "logout", "get-current-user-profile", "change-password",
                "get-all-terms", "get-active-term",
                "get-all-students", "get-student-by-id", "update-student-biodata", "get-student-id-card",
                "get-daily-attendance", "get-student-monthly-attendance", "batch-mark-attendance", "lock-attendance-register", "export-attendance-csv",
                "get-assessment-broadsheet", "batch-update-scores", "submit-broadsheet", "get-student-results", "export-broadsheet-results", "import-scores-csv", "get-score-template-csv",
                "get-fee-schedule",
                "get-all-requisitions", "get-requisition-by-id", "create-requisition",
                "get-scheme-of-work", "create-scheme-of-work", "update-scheme-of-work",
                "get-timetable",
                "get-all-lesson-notes", "create-lesson-note",
                "get-assignments", "create-assignment", "update-assignment", "get-assignment-submissions",
                "get-inquiries", "get-inquiry-by-id", "create-inquiry", "add-inquiry-message",
                "get-school-requirements", "toggle-requirement-status"
            },
            ["student"] = new List<string>
            {
                "logout", "get-current-user-profile", "change-password",
                "get-all-terms", "get-active-term",
                "get-student-by-id", "get-student-id-card",
                "get-student-monthly-attendance", "submit-absence-notice",
                "get-student-results",
                "get-fee-schedule", "get-student-fee-summary", "post-fee-teller", "get-student-receipts", "get-receipt-pdf", "get-bank-accounts", "get-student-statement",
                "get-scheme-of-work", "get-timetable",
                "get-assignments", "get-assignment-submissions", "submit-assignment",
                "get-inquiries", "get-inquiry-by-id", "create-inquiry", "add-inquiry-message",
                "get-school-requirements", "export-requirements-pdf"
            },
            ["parent"] = new List<string>
            {
                "logout", "get-current-user-profile", "change-password",
                "get-all-terms", "get-active-term",
                "get-student-by-id", "get-student-id-card",
                "get-student-monthly-attendance", "submit-absence-notice",
                "get-student-results",
                "get-fee-schedule", "get-student-fee-summary", "post-fee-teller", "get-student-receipts", "get-receipt-pdf", "get-bank-accounts", "get-student-statement",
                "get-timetable",
                "get-assignments",
                "get-inquiries", "get-inquiry-by-id", "create-inquiry", "add-inquiry-message",
                "get-school-requirements", "export-requirements-pdf"
            }
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
