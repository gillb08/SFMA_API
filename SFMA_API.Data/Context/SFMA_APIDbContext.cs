using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SFMA_API.Models.Entities;

namespace SFMA_API.Data.Context
{
    public class SFMA_APIDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string,
        ApplicationUserClaim, ApplicationUserRole, IdentityUserLogin<string>, ApplicationRoleClaim,
        IdentityUserToken<string>>
    {
        private readonly IHttpContextAccessor? _contextAccessor;

        public SFMA_APIDbContext(DbContextOptions<SFMA_APIDbContext> options, IHttpContextAccessor? contextAccessor = null)
            : base(options)
        {
            _contextAccessor = contextAccessor;
        }

        public virtual DbSet<AcademicTerm> AcademicTerms { get; set; } = null!;
        public virtual DbSet<Staff> Staff { get; set; } = null!;
        public virtual DbSet<Student> Students { get; set; } = null!;
        public virtual DbSet<Parent> Parents { get; set; } = null!;
        public virtual DbSet<StudentParent> StudentParents { get; set; } = null!;
        public virtual DbSet<ClassSection> ClassSections { get; set; } = null!;
        public virtual DbSet<AttendanceRecord> AttendanceRecords { get; set; } = null!;
        public virtual DbSet<AttendanceAuditLog> AttendanceAuditLogs { get; set; } = null!;
        public virtual DbSet<AssessmentScore> AssessmentScores { get; set; } = null!;
        public virtual DbSet<FeeSchedule> FeeSchedules { get; set; } = null!;
        public virtual DbSet<FeeItem> FeeItems { get; set; } = null!;
        public virtual DbSet<FeeTransaction> FeeTransactions { get; set; } = null!;
        public virtual DbSet<Receipt> Receipts { get; set; } = null!;
        public virtual DbSet<Requisition> Requisitions { get; set; } = null!;
        public virtual DbSet<ApprovalStage> ApprovalStages { get; set; } = null!;
        public virtual DbSet<SchemeOfWorkEntry> SchemeOfWorkEntries { get; set; } = null!;
        public virtual DbSet<TimetableSlot> TimetableSlots { get; set; } = null!;
        public virtual DbSet<LessonNote> LessonNotes { get; set; } = null!;
        public virtual DbSet<Assignment> Assignments { get; set; } = null!;
        public virtual DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; } = null!;
        public virtual DbSet<Inquiry> Inquiries { get; set; } = null!;
        public virtual DbSet<InquiryMessage> InquiryMessages { get; set; } = null!;
        public virtual DbSet<InquiryTimelineStep> InquiryTimelineSteps { get; set; } = null!;
        public virtual DbSet<SchoolRequirement> SchoolRequirements { get; set; } = null!;
        public virtual DbSet<RequirementStatus> RequirementStatuses { get; set; } = null!;
        public virtual DbSet<IdCard> IdCards { get; set; } = null!;
        public virtual DbSet<BankAccount> BankAccounts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasMany(e => e.Claims)
                    .WithOne(e => e.User)
                    .HasForeignKey(uc => uc.UserId)
                    .IsRequired();
                entity.HasMany(e => e.UserRoles)
                    .WithOne(e => e.User)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            modelBuilder.Entity<ApplicationRole>(entity =>
            {
                entity.HasIndex(r => r.Key).IsUnique();
                entity.HasMany(e => e.UserRoles)
                    .WithOne(e => e.Role)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();
                entity.HasMany(e => e.RoleClaims)
                    .WithOne(e => e.Role)
                    .HasForeignKey(rc => rc.RoleId)
                    .IsRequired();
            });

            modelBuilder.Entity<Staff>(entity =>
            {
                entity.HasIndex(s => s.UserId).IsUnique();
                entity.HasIndex(s => s.StaffCode).IsUnique();
                entity.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasIndex(s => s.UserId).IsUnique();
                entity.HasIndex(s => s.StudentCode).IsUnique();
                entity.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(s => s.ClassSection)
                    .WithMany(c => c.Students)
                    .HasForeignKey(s => s.ClassSectionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Parent>(entity =>
            {
                entity.HasIndex(p => p.UserId).IsUnique();
                entity.HasOne(p => p.User)
                    .WithMany()
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StudentParent>(entity =>
            {
                entity.HasKey(sp => new { sp.StudentId, sp.ParentId });
                entity.HasOne(sp => sp.Student)
                    .WithMany(s => s.StudentParents)
                    .HasForeignKey(sp => sp.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(sp => sp.Parent)
                    .WithMany(p => p.StudentParents)
                    .HasForeignKey(sp => sp.ParentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ClassSection>(entity =>
            {
                entity.HasOne(c => c.HomeroomTeacher)
                    .WithMany()
                    .HasForeignKey(c => c.HomeroomTeacherId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(c => c.AcademicTerm)
                    .WithMany(t => t.ClassSections)
                    .HasForeignKey(c => c.AcademicTermId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AttendanceRecord>(entity =>
            {
                entity.HasIndex(a => new { a.StudentId, a.Date }).IsUnique();
                entity.HasOne(a => a.Student)
                    .WithMany(s => s.AttendanceRecords)
                    .HasForeignKey(a => a.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.ClassSection)
                    .WithMany(c => c.AttendanceRecords)
                    .HasForeignKey(a => a.ClassSectionId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.RecordedBy)
                    .WithMany()
                    .HasForeignKey(a => a.RecordedById)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.AcademicTerm)
                    .WithMany(t => t.AttendanceRecords)
                    .HasForeignKey(a => a.AcademicTermId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AttendanceAuditLog>(entity =>
            {
                entity.HasOne(a => a.ClassSection)
                    .WithMany(c => c.AttendanceAuditLogs)
                    .HasForeignKey(a => a.ClassSectionId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.UnlockedBy)
                    .WithMany()
                    .HasForeignKey(a => a.UnlockedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AssessmentScore>(entity =>
            {
                entity.HasIndex(a => new { a.StudentId, a.Subject, a.AcademicTermId }).IsUnique();
                entity.Property(a => a.Ca1Score).HasColumnType("decimal(5,2)");
                entity.Property(a => a.Ca2Score).HasColumnType("decimal(5,2)");
                entity.Property(a => a.ProjectScore).HasColumnType("decimal(5,2)");
                entity.Property(a => a.ExamScore).HasColumnType("decimal(5,2)");
                entity.Property(a => a.TotalScore).HasColumnType("decimal(5,2)");

                entity.HasOne(a => a.Student)
                    .WithMany(s => s.AssessmentScores)
                    .HasForeignKey(a => a.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.AcademicTerm)
                    .WithMany(t => t.AssessmentScores)
                    .HasForeignKey(a => a.AcademicTermId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.SubmittedBy)
                    .WithMany()
                    .HasForeignKey(a => a.SubmittedById)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(a => a.ApprovedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FeeSchedule>(entity =>
            {
                entity.Property(f => f.TotalAmount).HasColumnType("decimal(12,2)");
                entity.HasOne(f => f.AcademicTerm)
                    .WithMany(t => t.FeeSchedules)
                    .HasForeignKey(f => f.AcademicTermId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(f => f.ClassSection)
                    .WithMany()
                    .HasForeignKey(f => f.ClassSectionId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<FeeItem>(entity =>
            {
                entity.Property(f => f.Amount).HasColumnType("decimal(12,2)");
                entity.HasOne(f => f.FeeSchedule)
                    .WithMany(fs => fs.FeeItems)
                    .HasForeignKey(f => f.FeeScheduleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FeeTransaction>(entity =>
            {
                entity.Property(f => f.AmountPaid).HasColumnType("decimal(12,2)");
                entity.HasOne(f => f.Student)
                    .WithMany(s => s.FeeTransactions)
                    .HasForeignKey(f => f.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(f => f.AcademicTerm)
                    .WithMany(t => t.FeeTransactions)
                    .HasForeignKey(f => f.AcademicTermId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(f => f.VerifiedBy)
                    .WithMany()
                    .HasForeignKey(f => f.VerifiedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Receipt>(entity =>
            {
                entity.HasIndex(r => r.ReceiptCode).IsUnique();
                entity.HasIndex(r => r.FeeTransactionId).IsUnique();
                entity.HasOne(r => r.FeeTransaction)
                    .WithOne(f => f.Receipt)
                    .HasForeignKey<Receipt>(r => r.FeeTransactionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(r => r.VerifiedBy)
                    .WithMany()
                    .HasForeignKey(r => r.VerifiedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Requisition>(entity =>
            {
                entity.HasIndex(r => r.RequisitionCode).IsUnique();
                entity.Property(r => r.Amount).HasColumnType("decimal(12,2)");
                entity.HasOne(r => r.RequestedBy)
                    .WithMany(s => s.Requisitions)
                    .HasForeignKey(r => r.RequestedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ApprovalStage>(entity =>
            {
                entity.HasOne(a => a.Requisition)
                    .WithMany(r => r.ApprovalStages)
                    .HasForeignKey(a => a.RequisitionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.ActedBy)
                    .WithMany()
                    .HasForeignKey(a => a.ActedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SchemeOfWorkEntry>(entity =>
            {
                entity.HasOne(s => s.ClassSection)
                    .WithMany(c => c.SchemeOfWorkEntries)
                    .HasForeignKey(s => s.ClassSectionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(s => s.AcademicTerm)
                    .WithMany(t => t.SchemeOfWorkEntries)
                    .HasForeignKey(s => s.AcademicTermId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TimetableSlot>(entity =>
            {
                entity.HasOne(t => t.ClassSection)
                    .WithMany(c => c.TimetableSlots)
                    .HasForeignKey(t => t.ClassSectionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(t => t.Teacher)
                    .WithMany()
                    .HasForeignKey(t => t.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<LessonNote>(entity =>
            {
                entity.HasIndex(l => l.NoteCode).IsUnique();
                entity.HasOne(l => l.Staff)
                    .WithMany(s => s.LessonNotes)
                    .HasForeignKey(l => l.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(l => l.ClassSection)
                    .WithMany(c => c.LessonNotes)
                    .HasForeignKey(l => l.ClassSectionId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(l => l.ReviewedBy)
                    .WithMany()
                    .HasForeignKey(l => l.ReviewedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Assignment>(entity =>
            {
                entity.HasIndex(a => a.AssignmentCode).IsUnique();
                entity.HasOne(a => a.Staff)
                    .WithMany(s => s.Assignments)
                    .HasForeignKey(a => a.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.ClassSection)
                    .WithMany(c => c.Assignments)
                    .HasForeignKey(a => a.ClassSectionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AssignmentSubmission>(entity =>
            {
                entity.HasOne(a => a.Assignment)
                    .WithMany(asg => asg.Submissions)
                    .HasForeignKey(a => a.AssignmentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.Student)
                    .WithMany(s => s.AssignmentSubmissions)
                    .HasForeignKey(a => a.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Inquiry>(entity =>
            {
                entity.HasIndex(i => i.InquiryCode).IsUnique();
                entity.HasOne(i => i.Parent)
                    .WithMany(p => p.Inquiries)
                    .HasForeignKey(i => i.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(i => i.Student)
                    .WithMany(s => s.Inquiries)
                    .HasForeignKey(i => i.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(i => i.RoutedTo)
                    .WithMany()
                    .HasForeignKey(i => i.RoutedToId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InquiryMessage>(entity =>
            {
                entity.HasOne(m => m.Inquiry)
                    .WithMany(i => i.Messages)
                    .HasForeignKey(m => m.InquiryId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InquiryTimelineStep>(entity =>
            {
                entity.HasOne(t => t.Inquiry)
                    .WithMany(i => i.TimelineSteps)
                    .HasForeignKey(t => t.InquiryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SchoolRequirement>(entity =>
            {
                entity.HasIndex(r => r.ItemCode).IsUnique();
                entity.HasOne(r => r.ClassSection)
                    .WithMany(c => c.SchoolRequirements)
                    .HasForeignKey(r => r.ClassSectionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(r => r.AcademicTerm)
                    .WithMany(t => t.SchoolRequirements)
                    .HasForeignKey(r => r.AcademicTermId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RequirementStatus>(entity =>
            {
                entity.HasOne(r => r.Requirement)
                    .WithMany(req => req.RequirementStatuses)
                    .HasForeignKey(r => r.RequirementId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(r => r.Student)
                    .WithMany(s => s.RequirementStatuses)
                    .HasForeignKey(r => r.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<IdCard>(entity =>
            {
                entity.HasIndex(c => c.StudentId).IsUnique();
                entity.HasIndex(c => c.CardSerial).IsUnique();
                entity.HasOne(c => c.Student)
                    .WithOne(s => s.IdCard)
                    .HasForeignKey<IdCard>(c => c.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
