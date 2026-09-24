using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
    public class UserProfileResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string PortalRedirect { get; set; } = string.Empty;
    }

    public class LoggedInUserResponse
    {
        public string Token { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public UserProfileResponse User { get; set; } = null!;
        public List<string> MenuItems { get; set; } = new List<string>();
        public List<string> Permissions { get; set; } = new List<string>();
    }

    public class MenuResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public int Order { get; set; }
        public List<string> Claims { get; set; } = new List<string>();
        public bool Active { get; set; }
    }

    public class TermResponse
    {
        public Guid Id { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public short TermNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime? Cat1Deadline { get; set; }
        public DateTime? Cat2Deadline { get; set; }
        public DateTime? ExamDeadline { get; set; }
    }

    public class DeadlinesResponse
    {
        public bool Success { get; set; } = true;
        public Guid TermId { get; set; }
        public DeadlinesDto Deadlines { get; set; } = new DeadlinesDto();
    }

    public class DeadlinesDto
    {
        public DateTime? Cat1Deadline { get; set; }
        public DateTime? Cat2Deadline { get; set; }
        public DateTime? ExamDeadline { get; set; }
    }

    public class StaffResponse
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string StaffCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime JoinedDate { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
    }

    public class CreateStaffResponse
    {
        public StaffResponse Staff { get; set; } = null!;
        public UserProfileResponse User { get; set; } = null!;
        public string TempPassword { get; set; } = string.Empty;
    }

    public class RoleResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
        public List<string> Claims { get; set; } = new List<string>();
    }

    public class StudentParentResponse
    {
        public Guid ParentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }

    public class StudentResponse
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public Guid ClassSectionId { get; set; }
        public string ClassSectionName { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public StudentStatus Status { get; set; }
        public DateTime AdmittedAt { get; set; }
        public List<StudentParentResponse> Parents { get; set; } = new List<StudentParentResponse>();
    }

    public class AdmitStudentResponse
    {
        public StudentResponse Student { get; set; } = null!;
        public UserProfileResponse ParentAccount { get; set; } = null!;
        public UserProfileResponse StudentAccount { get; set; } = null!;
        public IdCardResponse IdCard { get; set; } = null!;
    }

    public class IdCardResponse
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string CardSerial { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public bool NfcProvisioned { get; set; } = true;
        public IdCardStatus Status { get; set; }
    }

    public class AttendanceRecordResponse
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public DateTime Date { get; set; }
        public AttendanceStatus Status { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public string? Notes { get; set; }
        public string RecordedByName { get; set; } = string.Empty;
        public bool Locked { get; set; }
    }

    public class BatchUpdateAttendanceResponse
    {
        public int Updated { get; set; }
        public bool Synced { get; set; }
    }

    public class UnlockAttendanceResponse
    {
        public bool Success { get; set; } = true;
        public Guid ClassId { get; set; }
        public DateTime Date { get; set; }
        public string UnlockedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class AttendanceAuditLogResponse
    {
        public Guid Id { get; set; }
        public Guid ClassSectionId { get; set; }
        public DateTime Date { get; set; }
        public string UnlockedByName { get; set; } = string.Empty;
        public string RoleAtAction { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ComputedScoreResponse
    {
        public Guid StudentId { get; set; }
        public decimal Total { get; set; }
        public string Grade { get; set; } = string.Empty;
    }

    public class BatchUpdateAssessmentsResponse
    {
        public int Saved { get; set; }
        public List<ComputedScoreResponse> Computed { get; set; } = new List<ComputedScoreResponse>();
    }

    public class AssessmentScoreResponse
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public decimal? Ca1Score { get; set; }
        public decimal? Ca2Score { get; set; }
        public decimal? ProjectScore { get; set; }
        public decimal? ExamScore { get; set; }
        public decimal TotalScore { get; set; }
        public string Grade { get; set; } = string.Empty;
        public string? Remark { get; set; }
        public AssessmentStatus Status { get; set; }
        public string? ApprovedByName { get; set; }
    }

    public class FeeItemResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public short SortOrder { get; set; }
    }

    public class FeeScheduleResponse
    {
        public Guid Id { get; set; }
        public Guid AcademicTermId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<FeeItemResponse> FeeItems { get; set; } = new List<FeeItemResponse>();
    }

    public class FeeTransactionResponse
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public Guid AcademicTermId { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? BankTellerRef { get; set; }
        public FeeTransactionStatus Status { get; set; }
        public string? VerifiedByName { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StudentFeeSummaryResponse
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
        public List<FeeTransactionResponse> Transactions { get; set; } = new List<FeeTransactionResponse>();
    }

    public class ReceiptResponse
    {
        public Guid Id { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public Guid FeeTransactionId { get; set; }
        public DateTime IssuedAt { get; set; }
        public string VerifiedByName { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? PdfUrl { get; set; }
    }

    public class BankAccountResponse
    {
        public Guid Id { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string SortCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class ApprovalStageResponse
    {
        public Guid Id { get; set; }
        public ApprovalStageType Stage { get; set; }
        public ApprovalAction Action { get; set; }
        public string? ActedByName { get; set; }
        public DateTime? ActedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class RequisitionResponse
    {
        public Guid Id { get; set; }
        public string RequisitionCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public RequisitionPriority Priority { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public RequisitionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ApprovalStageResponse> ApprovalStages { get; set; } = new List<ApprovalStageResponse>();
    }

    public class SchemeOfWorkResponse
    {
        public Guid Id { get; set; }
        public Guid ClassSectionId { get; set; }
        public Guid AcademicTermId { get; set; }
        public short WeekNumber { get; set; }
        public string WeekLabel { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string? LearningObjectives { get; set; }
        public string? MaterialsName { get; set; }
        public string? MaterialsUrl { get; set; }
        public SchemeStatus Status { get; set; }
        public bool VettedByHos { get; set; }
    }

    public class TimetableSlotResponse
    {
        public Guid Id { get; set; }
        public Guid ClassSectionId { get; set; }
        public DayOfWeekEnum DayOfWeek { get; set; }
        public short PeriodNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Subject { get; set; }
        public Guid? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public bool IsBreak { get; set; }
    }

    public class LessonNoteResponse
    {
        public Guid Id { get; set; }
        public string NoteCode { get; set; } = string.Empty;
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public string? Objectives { get; set; }
        public string? FileUrl { get; set; }
        public LessonNoteStatus Status { get; set; }
        public string? ReviewedByName { get; set; }
        public string? ReviewerComments { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class AssignmentResponse
    {
        public Guid Id { get; set; }
        public string AssignmentCode { get; set; } = string.Empty;
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public DateTime DueDate { get; set; }
        public string SubmissionMode { get; set; } = "Any File Format";
        public AssignmentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SubmissionsCount { get; set; }
    }

    public class AssignmentSubmissionResponse
    {
        public Guid Id { get; set; }
        public Guid AssignmentId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int? FileSizeBytes { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string? Grade { get; set; }
    }

    public class InquiryMessageResponse
    {
        public Guid Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }

    public class InquiryTimelineStepResponse
    {
        public Guid Id { get; set; }
        public string StageLabel { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public short SortOrder { get; set; }
    }

    public class InquiryResponse
    {
        public Guid Id { get; set; }
        public string InquiryCode { get; set; } = string.Empty;
        public Guid ParentId { get; set; }
        public string ParentName { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubjectLine { get; set; } = string.Empty;
        public Guid RoutedToId { get; set; }
        public string RoutedToName { get; set; } = string.Empty;
        public InquiryStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<InquiryMessageResponse> Messages { get; set; } = new List<InquiryMessageResponse>();
        public List<InquiryTimelineStepResponse> TimelineSteps { get; set; } = new List<InquiryTimelineStepResponse>();
    }

    public class SchoolRequirementResponse
    {
        public Guid Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public Guid AcademicTermId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Author { get; set; }
        public string? Publisher { get; set; }
        public bool IsRequired { get; set; }
        public bool? Acquired { get; set; }
    }

    public class ExecutiveDashboardResponse
    {
        public int TotalEnrollment { get; set; }
        public decimal TotalRevenue { get; set; }
        public int RequisitionPipelineCount { get; set; }
        public int StaffCount { get; set; }
    }

    public class EarlyYearsSnapshot
    {
        public string Focus { get; set; } = "Early Literacy & Motor Skills";
        public decimal MilestoneAttainmentRate { get; set; } = 96.2m;
        public int EnrolledPupils { get; set; } = 78;
    }

    public class JuniorBasicSnapshot
    {
        public string Focus { get; set; } = "Cognitive & Terminal Assessment Broadsheet";
        public decimal AverageScore { get; set; } = 85.7m;
        public decimal PassRate { get; set; } = 100m;
        public int EvaluatedScholars { get; set; } = 6;
    }

    public class QualityAssuranceSnapshot
    {
        public decimal SchemesVettedRate { get; set; } = 100m;
        public string Standing { get; set; } = "Approved for Terminal Examinations";
    }

    public class AcademicSnapshotResponse
    {
        public string Term { get; set; } = "Term 2 2025/2026";
        public EarlyYearsSnapshot EarlyYears { get; set; } = new EarlyYearsSnapshot();
        public JuniorBasicSnapshot JuniorBasic { get; set; } = new JuniorBasicSnapshot();
        public QualityAssuranceSnapshot QualityAssurance { get; set; } = new QualityAssuranceSnapshot();
    }

    public class BudgetItemProgress
    {
        public string Category { get; set; } = string.Empty;
        public decimal BudgetedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal PercentageUsed => BudgetedAmount == 0 ? 0 : Math.Round((ActualAmount / BudgetedAmount) * 100, 1);
    }

    public class FinancialDashboardResponse
    {
        public decimal TotalBilled { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int UnpostedTellersCount { get; set; }
    }
}
