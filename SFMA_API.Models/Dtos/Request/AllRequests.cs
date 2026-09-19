using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class RefreshAccessTokenRequest
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class UpdateTermRequest
    {
        public string SessionName { get; set; } = string.Empty;
        public short TermNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class ConfigureDeadlinesRequest
    {
        public DateTime? Cat1Deadline { get; set; }
        public DateTime? Cat2Deadline { get; set; }
        public DateTime? ExamDeadline { get; set; }
    }

    public class CreateStaffRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string RoleKey { get; set; } = "teacher";
        public string Department { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class UpdateStaffRequest
    {
        public string Department { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? RoleKey { get; set; }
        public string? Phone { get; set; }
    }

    public class UpdateStaffStatusRequest
    {
        public string Status { get; set; } = "active";
    }

    public class AdmitStudentRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = "Male";
        public DateTime Dob { get; set; }
        public Guid ClassAdmitted { get; set; }
        public string GuardianName { get; set; } = string.Empty;
        public string GuardianPhone { get; set; } = string.Empty;
        public string GuardianEmail { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        /// <summary>Relationship of the guardian to the student e.g. Father, Mother, Guardian, Uncle</summary>
        public string GuardianRelationship { get; set; } = "Guardian";
    }

    public class UpdateStudentBioDataRequest
    {
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Address { get; set; }
    }

    public class BatchPrintRequest
    {
        public List<Guid> StudentIds { get; set; } = new List<Guid>();
    }

    public class AttendanceRecordItemRequest
    {
        public Guid StudentId { get; set; }
        public AttendanceStatus Status { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public string? Notes { get; set; }
    }

    public class BatchUpdateAttendanceRequest
    {
        public Guid ClassId { get; set; }
        public DateTime Date { get; set; }
        public List<AttendanceRecordItemRequest> Records { get; set; } = new List<AttendanceRecordItemRequest>();
    }

    public class LockAttendanceRequest
    {
        public Guid ClassId { get; set; }
        public DateTime Date { get; set; }
    }

    public class UnlockAttendanceRequest
    {
        public Guid ClassId { get; set; }
        public DateTime Date { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class AbsenceNoticeRequest
    {
        public Guid StudentId { get; set; }
        public DateTime Date { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class AssessmentScoreItemRequest
    {
        public Guid StudentId { get; set; }
        public decimal? Ca1 { get; set; }
        public decimal? Ca2 { get; set; }
        public decimal? Project { get; set; }
        public decimal? Exam { get; set; }
        public string? Remark { get; set; }
    }

    public class BatchUpdateAssessmentsRequest
    {
        public Guid ClassId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public Guid TermId { get; set; }
        public List<AssessmentScoreItemRequest> Scores { get; set; } = new List<AssessmentScoreItemRequest>();
    }

    public class BroadsheetActionRequest
    {
        public Guid ClassId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public Guid TermId { get; set; }
    }

    public class PostTellerRequest
    {
        public Guid StudentId { get; set; }
        public Guid? AcademicTermId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string BankTellerRef { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        /// <summary>Payment method e.g. Bank Transfer, Mobile Transfer, POS, Cash</summary>
        public string PaymentMethod { get; set; } = "Bank Transfer";
        public string? Notes { get; set; }
        public string? AttachmentFile { get; set; }
    }

    public class VerifyTellerRequest
    {
        public string Status { get; set; } = "verified";
        public string? Notes { get; set; }
    }

    public class QueuePaymentRemindersRequest
    {
        public Guid AcademicTermId { get; set; }
        public decimal? MinBalance { get; set; }
    }

    public class CreateRequisitionRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public RequisitionPriority Priority { get; set; } = RequisitionPriority.Medium;
    }

    public class ApproveRequisitionRequest
    {
        public string? Notes { get; set; }
    }

    public class QueryRequisitionRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class CreateSchemeOfWorkRequest
    {
        public Guid ClassSectionId { get; set; }
        public Guid AcademicTermId { get; set; }
        public short WeekNumber { get; set; }
        public string WeekLabel { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string? LearningObjectives { get; set; }
        public string? MaterialsName { get; set; }
        public string? MaterialsUrl { get; set; }
        public SchemeStatus? Status { get; set; }
    }

    public class UpdateSchemeOfWorkRequest
    {
        public string? Topic { get; set; }
        public string? LearningObjectives { get; set; }
        public string? MaterialsName { get; set; }
        public string? MaterialsUrl { get; set; }
        public SchemeStatus? Status { get; set; }
    }

    public class TimetableSlotRequest
    {
        public DayOfWeekEnum DayOfWeek { get; set; }
        public short PeriodNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Subject { get; set; }
        public Guid? TeacherId { get; set; }
        public bool IsBreak { get; set; }
    }

    public class UpdateTimetableRequest
    {
        public List<TimetableSlotRequest> Slots { get; set; } = new List<TimetableSlotRequest>();
    }

    public class CreateLessonNoteRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public string? Objectives { get; set; }
        public string? FileUrl { get; set; }
    }

    public class ReviewLessonNoteRequest
    {
        public LessonNoteStatus Status { get; set; }
        public string? ReviewerComments { get; set; }
    }

    public class CreateAssignmentRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public DateTime DueDate { get; set; }
        public string SubmissionMode { get; set; } = "Any File Format";
    }

    public class UpdateAssignmentRequest
    {
        public string? Subject { get; set; }
        public string? Title { get; set; }
        public string? Instructions { get; set; }
        public DateTime? DueDate { get; set; }
        public string? SubmissionMode { get; set; }
        public AssignmentStatus? Status { get; set; }
    }

    public class SubmitAssignmentRequest
    {
        public Guid StudentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int? FileSizeBytes { get; set; }
        public string? FileUrl { get; set; }
    }

    public class CreateInquiryRequest
    {
        public Guid StudentId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string SubjectLine { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class AddInquiryMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class UpdateInquiryStatusRequest
    {
        public InquiryStatus Status { get; set; }
    }

    public class ToggleRequirementStatusRequest
    {
        public Guid StudentId { get; set; }
        public bool Acquired { get; set; }
    }
}
