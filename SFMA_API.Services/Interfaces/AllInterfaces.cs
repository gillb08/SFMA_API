using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SFMA_API.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<LoggedInUserResponse> Login(LoginRequest request);
        Task<bool> Logout(string userId);
        Task<UserProfileResponse> GetCurrentUserProfile(string userId);
        Task<bool> ChangePassword(string userId, ChangePasswordRequest request);
        Task<bool> ResetPassword(ResetPasswordRequest request);
        Task<LoggedInUserResponse> RefreshAccessToken(RefreshAccessTokenRequest request);
    }

    public interface ITermService
    {
        Task<IEnumerable<TermResponse>> GetAllTerms();
        Task<TermResponse?> GetActiveTerm();
        Task<TermResponse> UpdateTerm(Guid id, UpdateTermRequest request);
        Task<DeadlinesResponse> ConfigureDeadlines(Guid id, ConfigureDeadlinesRequest request);
    }

    public interface IStaffService
    {
        Task<PagedResponse<StaffResponse>> GetAllStaff(string? department, string? roleKey, string? status, RequestParameters parameters);
        Task<StaffResponse?> GetStaffById(Guid id);
        Task<CreateStaffResponse> CreateStaff(CreateStaffRequest request);
        Task<StaffResponse> UpdateStaff(Guid id, UpdateStaffRequest request);
        Task<StaffResponse> UpdateStaffStatus(Guid id, UpdateStaffStatusRequest request);
        Task<string> AdminResetPassword(Guid id);
    }

    public interface IRolePermissionService
    {
        Task<IEnumerable<RoleResponse>> GetAllRoles();
        Task<RoleResponse?> GetRolePermissions(string roleKey);
        Task<IEnumerable<string>> GetRouteNames();
        Task<bool> UpdateRoleClaims(UpdateRoleClaimsRequest request);
        Task<bool> UpdateUserClaims(UpdateUserClaimsRequest request);
    }

    public interface IRoleClaimService : IRolePermissionService
    {
    }

    public interface IMenuService
    {
        Task<IEnumerable<MenuResponse>> GetAllMenus();
        Task<MenuResponse> CreateMenu(CreateMenuRequest request);
        Task<MenuResponse> UpdateMenu(Guid id, UpdateMenuRequest request);
        Task<bool> AddClaimsToMenu(AddClaimsToMenuRequest request);
        Task<IEnumerable<string>> GetMenuItems(IEnumerable<string> claims);
    }

    public interface IPermissionCacheService
    {
        Task<HashSet<string>> GetUserPermissionsAsync(string userId, Func<Task<HashSet<string>>> factory);
        void InvalidateUserPermissions(string userId);
        void InvalidateAll();
    }

    public interface IStudentService
    {
        Task<PagedResponse<StudentResponse>> GetAllStudents(Guid? classId, SFMA_API.Models.Enums.StudentStatus? status, RequestParameters parameters, ClaimsPrincipal currentUser);
        Task<StudentResponse?> GetStudentById(Guid id, ClaimsPrincipal currentUser);
        Task<AdmitStudentResponse> AdmitStudent(AdmitStudentRequest request);
        Task<StudentResponse> UpdateBioData(Guid id, UpdateStudentBioDataRequest request);
        Task<IdCardResponse?> GetIdCard(Guid studentId, ClaimsPrincipal currentUser);
        Task<byte[]> BatchPrint(BatchPrintRequest request);
    }

    public interface IAttendanceService
    {
        Task<IEnumerable<AttendanceRecordResponse>> GetAttendance(Guid classId, DateTime date, ClaimsPrincipal currentUser);
        Task<IEnumerable<AttendanceRecordResponse>> GetStudentMonthlyAttendance(Guid studentId, int month, int year, ClaimsPrincipal currentUser);
        Task<BatchUpdateAttendanceResponse> BatchUpdateAttendance(BatchUpdateAttendanceRequest request, ClaimsPrincipal currentUser);
        Task<bool> LockAttendance(LockAttendanceRequest request, ClaimsPrincipal currentUser);
        Task<UnlockAttendanceResponse> UnlockAttendance(UnlockAttendanceRequest request, ClaimsPrincipal currentUser);
        Task<IEnumerable<AttendanceAuditLogResponse>> GetAuditLogs(Guid? classSectionId, DateTime? date);
        Task<string> ExportAttendanceCsv(Guid classId, DateTime startDate, DateTime endDate);
        Task<bool> SubmitAbsenceNotice(AbsenceNoticeRequest request, ClaimsPrincipal currentUser);
    }

    public interface IAssessmentService
    {
        Task<IEnumerable<AssessmentScoreResponse>> GetBroadsheet(Guid classId, string subject, Guid termId, ClaimsPrincipal currentUser);
        Task<BatchUpdateAssessmentsResponse> BatchUpdateScores(BatchUpdateAssessmentsRequest request, ClaimsPrincipal currentUser);
        Task<bool> SubmitBroadsheet(BroadsheetActionRequest request, ClaimsPrincipal currentUser);
        Task<bool> ApproveBroadsheet(BroadsheetActionRequest request, ClaimsPrincipal currentUser);
        Task<IEnumerable<AssessmentScoreResponse>> GetStudentResults(Guid studentId, Guid? termId, ClaimsPrincipal currentUser);
        Task<string> ExportResults(Guid classId, string subject, Guid termId, string format);
        Task<BatchUpdateAssessmentsResponse> ImportScoresCsv(Guid classId, string subject, Guid termId, string csvContent, ClaimsPrincipal currentUser);
        Task<string> GetScoreTemplateCsv();
    }

    public interface IFeeService
    {
        Task<FeeScheduleResponse?> GetFeeSchedule(Guid termId, Guid? classSectionId);
        Task<PagedResponse<FeeTransactionResponse>> GetFeeLedger(SFMA_API.Models.Enums.FeeTransactionStatus? status, RequestParameters parameters);
        Task<StudentFeeSummaryResponse> GetStudentFeeSummary(Guid studentId, ClaimsPrincipal currentUser);
        Task<FeeTransactionResponse> PostTeller(PostTellerRequest request, ClaimsPrincipal currentUser);
        Task<FeeTransactionResponse> VerifyTeller(Guid id, VerifyTellerRequest request, ClaimsPrincipal currentUser);
        Task<IEnumerable<ReceiptResponse>> GetStudentReceipts(Guid studentId, ClaimsPrincipal currentUser);
        Task<ReceiptResponse?> GetReceiptPdf(Guid receiptId, ClaimsPrincipal currentUser);
        Task<bool> QueuePaymentReminders(QueuePaymentRemindersRequest request);
        Task<IEnumerable<BankAccountResponse>> GetBankAccounts();
        Task<StudentFeeSummaryResponse> GetStatementOfAccount(Guid studentId, ClaimsPrincipal currentUser);
    }

    public interface IRequisitionService
    {
        Task<PagedResponse<RequisitionResponse>> GetRequisitions(SFMA_API.Models.Enums.RequisitionStatus? status, string? department, RequestParameters parameters, ClaimsPrincipal currentUser);
        Task<RequisitionResponse?> GetRequisitionById(Guid id, ClaimsPrincipal currentUser);
        Task<RequisitionResponse> CreateRequisition(CreateRequisitionRequest request, ClaimsPrincipal currentUser);
        Task<RequisitionResponse> ApproveRequisition(Guid id, ApproveRequisitionRequest request, ClaimsPrincipal currentUser);
        Task<RequisitionResponse> QueryRequisition(Guid id, QueryRequisitionRequest request, ClaimsPrincipal currentUser);
    }

    public interface ISchemeAndTimetableService
    {
        Task<IEnumerable<SchemeOfWorkResponse>> GetSchemeOfWork(Guid classSectionId, Guid? termId);
        Task<SchemeOfWorkResponse> CreateSchemeOfWork(CreateSchemeOfWorkRequest request);
        Task<SchemeOfWorkResponse> UpdateSchemeOfWork(Guid id, UpdateSchemeOfWorkRequest request);
        Task<bool> VetSchemeOfWork(Guid id, ClaimsPrincipal currentUser);
        Task<IEnumerable<TimetableSlotResponse>> GetTimetable(Guid classId);
        Task<bool> UpdateTimetable(Guid classId, UpdateTimetableRequest request);
    }

    public interface ILessonNoteAndAssignmentService
    {
        Task<IEnumerable<LessonNoteResponse>> GetLessonNotes(SFMA_API.Models.Enums.LessonNoteStatus? status, ClaimsPrincipal currentUser);
        Task<LessonNoteResponse> CreateLessonNote(CreateLessonNoteRequest request, ClaimsPrincipal currentUser);
        Task<LessonNoteResponse> ReviewLessonNote(Guid id, ReviewLessonNoteRequest request, ClaimsPrincipal currentUser);
        Task<IEnumerable<AssignmentResponse>> GetAssignments(Guid classSectionId, ClaimsPrincipal currentUser);
        Task<AssignmentResponse> CreateAssignment(CreateAssignmentRequest request, ClaimsPrincipal currentUser);
        Task<AssignmentResponse> UpdateAssignment(Guid id, UpdateAssignmentRequest request);
        Task<IEnumerable<AssignmentSubmissionResponse>> GetAssignmentSubmissions(Guid assignmentId, ClaimsPrincipal currentUser);
        Task<AssignmentSubmissionResponse> SubmitAssignment(Guid assignmentId, SubmitAssignmentRequest request, ClaimsPrincipal currentUser);
    }

    public interface IInquiryAndRequirementService
    {
        Task<PagedResponse<InquiryResponse>> GetInquiries(SFMA_API.Models.Enums.InquiryStatus? status, RequestParameters parameters, ClaimsPrincipal currentUser);
        Task<InquiryResponse?> GetInquiryById(Guid id, ClaimsPrincipal currentUser);
        Task<InquiryResponse> CreateInquiry(CreateInquiryRequest request, ClaimsPrincipal currentUser);
        Task<InquiryMessageResponse> AddInquiryMessage(Guid id, AddInquiryMessageRequest request, ClaimsPrincipal currentUser);
        Task<InquiryResponse> UpdateInquiryStatus(Guid id, UpdateInquiryStatusRequest request, ClaimsPrincipal currentUser);
        Task<IEnumerable<SchoolRequirementResponse>> GetSchoolRequirements(Guid classSectionId, Guid? termId, Guid? studentId);
        Task<bool> ToggleRequirementStatus(Guid requirementId, ToggleRequirementStatusRequest request, ClaimsPrincipal currentUser);
        Task<string> ExportRequirementsPdf(Guid classSectionId);
    }

    public interface IDashboardService
    {
        Task<ExecutiveDashboardResponse> GetExecutiveDashboard();
        Task<AcademicSnapshotResponse> GetAcademicSnapshot();
        Task<IEnumerable<BudgetItemProgress>> GetBudgetProgress();
        Task<FinancialDashboardResponse> GetFinancialDashboard();
    }
}
