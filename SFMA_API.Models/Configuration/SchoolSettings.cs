using System.Collections.Generic;

namespace SFMA_API.Models.Configuration
{
    public class SchoolSettings
    {
        /// <summary>Email domain used for auto-generated student/parent accounts. e.g. stfaithacademy.edu.ng</summary>
        public string EmailDomain { get; set; } = string.Empty;

        /// <summary>Prefix used when generating student codes. e.g. SF produces SF-2026-0001</summary>
        public string StudentCodePrefix { get; set; } = "SF";

        /// <summary>Identity token provider name registered in Program.cs (must match AddRefreshTokenProvider registration)</summary>
        public string TokenProviderName { get; set; } = string.Empty;

        /// <summary>Roles that are treated as supervisors in academic workflows</summary>
        public List<string> SupervisorRoles { get; set; } = new();

        /// <summary>Maps role key → frontend portal URL returned after login</summary>
        public Dictionary<string, string> PortalRoutes { get; set; } = new();

        /// <summary>Ordered list of grade bands. First matching MinScore wins. Must be ordered descending by MinScore.</summary>
        public List<GradeBand> GradingScale { get; set; } = new();

        /// <summary>Minimum total score (inclusive) to be counted as a pass for dashboard pass-rate KPI</summary>
        public decimal PassMarkThreshold { get; set; } = 50;

        /// <summary>Minimum scheme-vetting percentage to show "Approved" standing on dashboard</summary>
        public decimal QualityAssuranceVettingThreshold { get; set; } = 80;

        /// <summary>
        /// Fallback budget amount shown when no DepartmentBudget record exists for a department.
        /// Set to 0 to surface unbudgeted departments clearly rather than showing a made-up number.
        /// </summary>
        public decimal DefaultDepartmentBudget { get; set; } = 0;

        /// <summary>Resolves the portal redirect path for a role. Returns /portal if the role has no configured path.</summary>
        public string GetPortalRoute(string role)
        {
            return PortalRoutes.TryGetValue(role, out var path) ? path : "/portal";
        }

        /// <summary>Resolves the letter grade for a given total score using the configured grading scale.</summary>
        public string ResolveGrade(decimal totalScore)
        {
            foreach (var band in GradingScale)
            {
                if (totalScore >= band.MinScore)
                    return band.Grade;
            }
            return "F";
        }
    }

    public class GradeBand
    {
        public decimal MinScore { get; set; }
        public string Grade { get; set; } = string.Empty;
    }
}

