using AutoMapper;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using System.Linq;

namespace SFMA_API.Models.Configuration.MappingConfiguration
{
    public class SfmaMappingProfile : Profile
    {
        public SfmaMappingProfile()
        {
            CreateMap<ApplicationUser, UserProfileResponse>()
                .ForMember(d => d.Role, opt => opt.MapFrom(s => s.UserRoles.FirstOrDefault() != null ? s.UserRoles.FirstOrDefault()!.Role.Key : string.Empty));

            CreateMap<ApplicationRole, RoleResponse>()
                .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.RoleClaims.Where(rc => rc.Active && !string.IsNullOrEmpty(rc.ClaimValue)).Select(rc => rc.ClaimValue).ToList()));

            CreateMap<Menu, MenuResponse>()
                .ForMember(d => d.Claims, opt => opt.MapFrom(s => !string.IsNullOrEmpty(s.ClaimsJson) ? System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(s.ClaimsJson, (System.Text.Json.JsonSerializerOptions?)null) ?? new System.Collections.Generic.List<string>() : new System.Collections.Generic.List<string>()));

            CreateMap<AcademicTerm, TermResponse>();
            CreateMap<UpdateTermRequest, AcademicTerm>();

            CreateMap<Staff, StaffResponse>()
                .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.User != null ? s.User.DisplayName : string.Empty))
                .ForMember(d => d.Email, opt => opt.MapFrom(s => s.User != null ? s.User.Email : string.Empty))
                .ForMember(d => d.Role, opt => opt.MapFrom(s => s.User != null && s.User.UserRoles.FirstOrDefault() != null ? s.User.UserRoles.FirstOrDefault()!.Role.Key : string.Empty))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.User != null ? s.User.Status.ToString() : "Active"));

            CreateMap<Student, StudentResponse>()
                .ForMember(d => d.ClassSectionName, opt => opt.MapFrom(s => s.ClassSection != null ? s.ClassSection.Name : string.Empty))
                .ForMember(d => d.Parents, opt => opt.MapFrom(s => s.StudentParents.Select(sp => new StudentParentResponse
                {
                    ParentId = sp.ParentId,
                    FullName = sp.Parent.FullName,
                    Phone = sp.Parent.Phone,
                    Email = sp.Parent.Email,
                    Relationship = sp.Relationship,
                    IsPrimary = sp.IsPrimary
                })));

            CreateMap<IdCard, IdCardResponse>()
                .ForMember(d => d.StudentName, opt => opt.MapFrom(s => s.Student != null ? s.Student.FullName : string.Empty))
                .ForMember(d => d.StudentCode, opt => opt.MapFrom(s => s.Student != null ? s.Student.StudentCode : string.Empty));

            CreateMap<AttendanceRecord, AttendanceRecordResponse>()
                .ForMember(d => d.StudentName, opt => opt.MapFrom(s => s.Student != null ? s.Student.FullName : string.Empty))
                .ForMember(d => d.StudentCode, opt => opt.MapFrom(s => s.Student != null ? s.Student.StudentCode : string.Empty))
                .ForMember(d => d.RecordedByName, opt => opt.MapFrom(s => s.RecordedBy != null && s.RecordedBy.User != null ? s.RecordedBy.User.DisplayName : string.Empty));

            CreateMap<AttendanceAuditLog, AttendanceAuditLogResponse>()
                .ForMember(d => d.UnlockedByName, opt => opt.MapFrom(s => s.UnlockedBy != null && s.UnlockedBy.User != null ? s.UnlockedBy.User.DisplayName : string.Empty));

            CreateMap<AssessmentScore, AssessmentScoreResponse>()
                .ForMember(d => d.StudentName, opt => opt.MapFrom(s => s.Student != null ? s.Student.FullName : string.Empty))
                .ForMember(d => d.StudentCode, opt => opt.MapFrom(s => s.Student != null ? s.Student.StudentCode : string.Empty))
                .ForMember(d => d.ApprovedByName, opt => opt.MapFrom(s => s.ApprovedBy != null && s.ApprovedBy.User != null ? s.ApprovedBy.User.DisplayName : string.Empty));

            CreateMap<FeeSchedule, FeeScheduleResponse>();
            CreateMap<FeeItem, FeeItemResponse>();

            CreateMap<FeeTransaction, FeeTransactionResponse>()
                .ForMember(d => d.StudentName, opt => opt.MapFrom(s => s.Student != null ? s.Student.FullName : string.Empty))
                .ForMember(d => d.StudentCode, opt => opt.MapFrom(s => s.Student != null ? s.Student.StudentCode : string.Empty))
                .ForMember(d => d.VerifiedByName, opt => opt.MapFrom(s => s.VerifiedBy != null && s.VerifiedBy.User != null ? s.VerifiedBy.User.DisplayName : string.Empty));

            CreateMap<Receipt, ReceiptResponse>()
                .ForMember(d => d.VerifiedByName, opt => opt.MapFrom(s => s.VerifiedBy != null && s.VerifiedBy.User != null ? s.VerifiedBy.User.DisplayName : string.Empty))
                .ForMember(d => d.AmountPaid, opt => opt.MapFrom(s => s.FeeTransaction != null ? s.FeeTransaction.AmountPaid : 0))
                .ForMember(d => d.StudentName, opt => opt.MapFrom(s => s.FeeTransaction != null && s.FeeTransaction.Student != null ? s.FeeTransaction.Student.FullName : string.Empty));

            CreateMap<BankAccount, BankAccountResponse>();

            CreateMap<Requisition, RequisitionResponse>()
                .ForMember(d => d.RequestedByName, opt => opt.MapFrom(s => s.RequestedBy != null && s.RequestedBy.User != null ? s.RequestedBy.User.DisplayName : string.Empty));

            CreateMap<ApprovalStage, ApprovalStageResponse>()
                .ForMember(d => d.ActedByName, opt => opt.MapFrom(s => s.ActedBy != null && s.ActedBy.User != null ? s.ActedBy.User.DisplayName : string.Empty));

            CreateMap<SchemeOfWorkEntry, SchemeOfWorkResponse>();
            CreateMap<CreateSchemeOfWorkRequest, SchemeOfWorkEntry>();

            CreateMap<TimetableSlot, TimetableSlotResponse>()
                .ForMember(d => d.TeacherName, opt => opt.MapFrom(s => s.Teacher != null && s.Teacher.User != null ? s.Teacher.User.DisplayName : string.Empty));

            CreateMap<LessonNote, LessonNoteResponse>()
                .ForMember(d => d.StaffName, opt => opt.MapFrom(s => s.Staff != null && s.Staff.User != null ? s.Staff.User.DisplayName : string.Empty))
                .ForMember(d => d.ReviewedByName, opt => opt.MapFrom(s => s.ReviewedBy != null && s.ReviewedBy.User != null ? s.ReviewedBy.User.DisplayName : string.Empty));

            CreateMap<Assignment, AssignmentResponse>()
                .ForMember(d => d.StaffName, opt => opt.MapFrom(s => s.Staff != null && s.Staff.User != null ? s.Staff.User.DisplayName : string.Empty))
                .ForMember(d => d.SubmissionsCount, opt => opt.MapFrom(s => s.Submissions.Count));

            CreateMap<AssignmentSubmission, AssignmentSubmissionResponse>()
                .ForMember(d => d.StudentName, opt => opt.MapFrom(s => s.Student != null ? s.Student.FullName : string.Empty));

            CreateMap<Inquiry, InquiryResponse>()
                .ForMember(d => d.ParentName, opt => opt.MapFrom(s => s.Parent != null ? s.Parent.FullName : string.Empty))
                .ForMember(d => d.StudentName, opt => opt.MapFrom(s => s.Student != null ? s.Student.FullName : string.Empty))
                .ForMember(d => d.RoutedToName, opt => opt.MapFrom(s => s.RoutedTo != null && s.RoutedTo.User != null ? s.RoutedTo.User.DisplayName : string.Empty));

            CreateMap<InquiryMessage, InquiryMessageResponse>()
                .ForMember(d => d.SenderName, opt => opt.MapFrom(s => s.Sender != null ? s.Sender.DisplayName : string.Empty));

            CreateMap<InquiryTimelineStep, InquiryTimelineStepResponse>();

            CreateMap<SchoolRequirement, SchoolRequirementResponse>();
        }
    }
}
