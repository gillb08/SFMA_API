namespace SFMA_API.Models.Enums
{
    public enum UserStatus
    {
        Active = 1,
        Suspended = 2,
        Locked = 3
    }

    public enum StudentStatus
    {
        Admitted = 1,
        Enrolled = 2,
        Graduated = 3,
        Withdrawn = 4
    }

    public enum AttendanceStatus
    {
        Present = 1,
        Late = 2,
        Absent = 3
    }

    public enum AssessmentStatus
    {
        Draft = 1,
        Submitted = 2,
        Approved = 3,
        Published = 4
    }

    public enum FeeTransactionStatus
    {
        Unposted = 1,
        PendingReview = 2,
        Verified = 3,
        Cleared = 4,
        Rejected = 5
    }

    public enum RequisitionPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum RequisitionStatus
    {
        Draft = 1,
        Submitted = 2,
        InReview = 3,
        Approved = 4,
        Disbursed = 5,
        Queried = 6,
        Rejected = 7
    }

    public enum ApprovalStageType
    {
        AcademicHead = 1,
        FinancialHead = 2,
        SuperAdmin = 3
    }

    public enum ApprovalAction
    {
        Pending = 1,
        Approved = 2,
        Queried = 3,
        Rejected = 4
    }

    public enum SchemeStatus
    {
        Upcoming = 1,
        InProgress = 2,
        Completed = 3
    }

    public enum LessonNoteStatus
    {
        Submitted = 1,
        UnderReview = 2,
        Approved = 3,
        RevisionRequested = 4
    }

    public enum AssignmentStatus
    {
        Draft = 1,
        Published = 2,
        Closed = 3
    }

    public enum InquiryStatus
    {
        Open = 1,
        InProgress = 2,
        Resolved = 3,
        Closed = 4
    }

    public enum IdCardStatus
    {
        Generated = 1,
        Printed = 2,
        Issued = 3,
        Deactivated = 4
    }

    public enum Gender
    {
        Male = 1,
        Female = 2
    }

    public enum DayOfWeekEnum
    {
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5
    }
}
