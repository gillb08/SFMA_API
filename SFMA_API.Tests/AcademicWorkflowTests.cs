using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using SFMA_API.Data.Context;
using SFMA_API.Data.Implementation;
using SFMA_API.Data.Interfaces;
using SFMA_API.Models.Configuration;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Entities;
using SFMA_API.Models.Enums;
using SFMA_API.Services.Implementation;
using SFMA_API.Services.Interfaces;
using System.Security.Claims;
using Xunit;

namespace SFMA_API.Tests
{
    public class AcademicWorkflowTests : IDisposable
    {
        private readonly SFMA_APIDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly SchemeAndTimetableService _schemeService;
        private readonly LessonNoteAndAssignmentService _lessonService;
        private readonly CurriculumTopicService _curriculumService;
        private readonly Mock<ISchoolNotificationService> _mockNotificationService;

        public AcademicWorkflowTests()
        {
            var options = new DbContextOptionsBuilder<SFMA_APIDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SFMA_APIDbContext(options);
            _unitOfWork = new UnitOfWork<SFMA_APIDbContext>(_context);
            _mockNotificationService = new Mock<ISchoolNotificationService>();

            var schoolSettings = Options.Create(new SchoolSettings
            {
                SupervisorRoles = new List<string> { "super_admin", "academic_head", "academic_admin", "admin" }
            });

            _schemeService = new SchemeAndTimetableService(_unitOfWork, null!);
            _lessonService = new LessonNoteAndAssignmentService(_unitOfWork, null!, schoolSettings, _mockNotificationService.Object);
            _curriculumService = new CurriculumTopicService(_unitOfWork);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private ClaimsPrincipal CreatePrincipal(string userId, string role, string name = "Test User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("Id", userId),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private async Task<(AcademicTerm term, ClassSection sectionA, ClassSection sectionB, Staff teacher1, Staff supervisor, Student studentA, Parent parentA)> SeedBaseDataAsync()
        {
            var term = new AcademicTerm
            {
                Id = Guid.NewGuid(),
                SessionName = "2026/2027",
                TermNumber = 1,
                IsActive = true,
                Active = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.AcademicTerms.AddAsync(term);

            var user1 = new ApplicationUser { Id = "user-teacher-1", UserName = "teacher1", DisplayName = "Teacher One", Active = true };
            var user2 = new ApplicationUser { Id = "user-teacher-2", UserName = "teacher2", DisplayName = "Teacher Two", Active = true };
            var userSup = new ApplicationUser { Id = "user-supervisor", UserName = "supervisor", DisplayName = "Supervisor Jane", Active = true };
            var userStudentA = new ApplicationUser { Id = "user-student-a", UserName = "studentA", DisplayName = "Student Alice", Active = true };
            var userParentA = new ApplicationUser { Id = "user-parent-a", UserName = "parentA", DisplayName = "Parent Alice", Active = true };
            await _context.Users.AddRangeAsync(user1, user2, userSup, userStudentA, userParentA);

            var teacher1 = new Staff { Id = Guid.NewGuid(), UserId = user1.Id, Department = "Primary Division", Active = true, CreatedAt = DateTime.UtcNow };
            var teacher2 = new Staff { Id = Guid.NewGuid(), UserId = user2.Id, Department = "Primary Division", Active = true, CreatedAt = DateTime.UtcNow };
            var supervisor = new Staff { Id = Guid.NewGuid(), UserId = userSup.Id, Department = "Academic Management", Active = true, CreatedAt = DateTime.UtcNow };
            await _context.Staff.AddRangeAsync(teacher1, teacher2, supervisor);

            var sectionA = new ClassSection
            {
                Id = Guid.NewGuid(),
                Name = "Primary 1 Gold",
                AcademicTermId = term.Id,
                HomeroomTeacherId = teacher1.Id,
                Active = true,
                CreatedAt = DateTime.UtcNow
            };

            var sectionB = new ClassSection
            {
                Id = Guid.NewGuid(),
                Name = "Primary 2 Silver",
                AcademicTermId = term.Id,
                HomeroomTeacherId = teacher2.Id,
                Active = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassSections.AddRangeAsync(sectionA, sectionB);

            var studentA = new Student
            {
                Id = Guid.NewGuid(),
                UserId = userStudentA.Id,
                ClassSectionId = sectionA.Id,
                StudentCode = "ADM001",
                FullName = "Alice Student",
                Active = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Students.AddAsync(studentA);

            var parentA = new Parent
            {
                Id = Guid.NewGuid(),
                UserId = userParentA.Id,
                FullName = "Alice Parent",
                Active = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Parents.AddAsync(parentA);

            var spLink = new StudentParent
            {
                StudentId = studentA.Id,
                ParentId = parentA.Id,
                Relationship = "Mother",
                IsPrimary = true
            };
            await _context.StudentParents.AddAsync(spLink);

            var subject = new Subject
            {
                Id = Guid.NewGuid(),
                Name = "Mathematics",
                Code = "MATH101",
                Active = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Subjects.AddAsync(subject);

            await _context.SaveChangesAsync();
            return (term, sectionA, sectionB, teacher1, supervisor, studentA, parentA);
        }

        [Fact]
        public async Task Teacher_Cannot_Create_Scheme_For_Another_Class()
        {
            var (term, sectionA, sectionB, teacher1, _, _, _) = await SeedBaseDataAsync();
            var teacher1Principal = CreatePrincipal("user-teacher-1", "teacher", "Teacher One");

            var request = new CreateSchemeOfWorkRequest
            {
                ClassSectionId = sectionB.Id, // Teacher 1 is assigned to Section A, not B
                AcademicTermId = term.Id,
                WeekNumber = 1,
                Topic = "Introduction to Fractions",
                LearningObjectives = "Understand half and quarters"
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _schemeService.CreateSchemeOfWork(request, teacher1Principal));
        }

        [Fact]
        public async Task Teacher_Can_Create_Scheme_For_Assigned_Class_With_Submitted_Status()
        {
            var (term, sectionA, _, teacher1, _, _, _) = await SeedBaseDataAsync();
            var teacher1Principal = CreatePrincipal("user-teacher-1", "teacher", "Teacher One");

            var request = new CreateSchemeOfWorkRequest
            {
                ClassSectionId = sectionA.Id,
                AcademicTermId = term.Id,
                WeekNumber = 1,
                Topic = "Number Counting 1-100",
                LearningObjectives = "Count forwards and backwards"
            };

            var result = await _schemeService.CreateSchemeOfWork(request, teacher1Principal);

            Assert.NotNull(result);
            Assert.Equal(SchemeApprovalStatus.Submitted, result.ApprovalStatus);
            Assert.False(result.VettedByHos);

            var auditLog = await _context.AcademicWorkflowAuditLogs.FirstOrDefaultAsync(l => l.EntityId == result.Id);
            Assert.NotNull(auditLog);
            Assert.Equal("Submitted", auditLog.Action);
            Assert.Equal(teacher1.Id, auditLog.ActorStaffId);
        }

        [Fact]
        public async Task Non_Supervisor_Cannot_Approve_Scheme()
        {
            var (term, sectionA, _, _, _, _, _) = await SeedBaseDataAsync();
            var teacher1Principal = CreatePrincipal("user-teacher-1", "teacher", "Teacher One");

            var scheme = new SchemeOfWorkEntry
            {
                Id = Guid.NewGuid(),
                ClassSectionId = sectionA.Id,
                AcademicTermId = term.Id,
                WeekNumber = 1,
                Topic = "Shapes & Geometry",
                ApprovalStatus = SchemeApprovalStatus.Submitted,
                VettedByHos = false,
                CreatedAt = DateTime.UtcNow
            };
            await _context.SchemeOfWorkEntries.AddAsync(scheme);
            await _context.SaveChangesAsync();

            var reviewReq = new ReviewSchemeOfWorkRequest { Action = SchemeApprovalStatus.Approved };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _schemeService.ReviewSchemeOfWork(scheme.Id, reviewReq, teacher1Principal));
        }

        [Fact]
        public async Task Supervisor_RequestRevision_Requires_Mandatory_Comment()
        {
            var (term, sectionA, _, _, _, _, _) = await SeedBaseDataAsync();
            var supervisorPrincipal = CreatePrincipal("user-supervisor", "academic_head", "Supervisor Jane");

            var scheme = new SchemeOfWorkEntry
            {
                Id = Guid.NewGuid(),
                ClassSectionId = sectionA.Id,
                AcademicTermId = term.Id,
                WeekNumber = 1,
                Topic = "Fractions",
                ApprovalStatus = SchemeApprovalStatus.Submitted,
                VettedByHos = false,
                CreatedAt = DateTime.UtcNow
            };
            await _context.SchemeOfWorkEntries.AddAsync(scheme);
            await _context.SaveChangesAsync();

            var reviewReq = new ReviewSchemeOfWorkRequest
            {
                Action = SchemeApprovalStatus.RevisionRequested,
                ReviewComment = "" // Missing mandatory comment
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _schemeService.ReviewSchemeOfWork(scheme.Id, reviewReq, supervisorPrincipal));
        }

        [Fact]
        public async Task LessonNote_Creation_Fails_Without_Approved_Scheme()
        {
            var (term, sectionA, _, teacher1, _, _, _) = await SeedBaseDataAsync();
            var teacher1Principal = CreatePrincipal("user-teacher-1", "teacher", "Teacher One");

            var unapprovedScheme = new SchemeOfWorkEntry
            {
                Id = Guid.NewGuid(),
                ClassSectionId = sectionA.Id,
                AcademicTermId = term.Id,
                WeekNumber = 2,
                Topic = "Multiplication",
                ApprovalStatus = SchemeApprovalStatus.Submitted,
                VettedByHos = false,
                CreatedAt = DateTime.UtcNow
            };
            await _context.SchemeOfWorkEntries.AddAsync(unapprovedScheme);
            await _context.SaveChangesAsync();

            var lessonReq = new CreateLessonNoteRequest
            {
                SchemeOfWorkEntryId = unapprovedScheme.Id,
                Objectives = "Learn 2 times table"
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _lessonService.CreateLessonNote(lessonReq, teacher1Principal));

            Assert.Contains("approved scheme of work entry", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LessonNote_Creation_Succeeds_With_Approved_Scheme_And_Delivery_Updates_Progress()
        {
            var (term, sectionA, _, teacher1, supervisor, _, _) = await SeedBaseDataAsync();
            var teacher1Principal = CreatePrincipal("user-teacher-1", "teacher", "Teacher One");
            var supervisorPrincipal = CreatePrincipal("user-supervisor", "academic_head", "Supervisor Jane");

            // 1. Create and approve scheme
            var scheme = new SchemeOfWorkEntry
            {
                Id = Guid.NewGuid(),
                ClassSectionId = sectionA.Id,
                AcademicTermId = term.Id,
                WeekNumber = 3,
                Topic = "Division Basics",
                ApprovalStatus = SchemeApprovalStatus.Approved,
                VettedByHos = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.SchemeOfWorkEntries.AddAsync(scheme);
            await _context.SaveChangesAsync();

            // 2. Create lesson note
            var lessonReq = new CreateLessonNoteRequest
            {
                SchemeOfWorkEntryId = scheme.Id,
                Objectives = "Divide whole numbers by 2 and 3",
                ContentSummary = "Grouping beads into sets"
            };

            var createdNote = await _lessonService.CreateLessonNote(lessonReq, teacher1Principal);
            Assert.NotNull(createdNote);
            Assert.Equal(scheme.Id, createdNote.SchemeOfWorkEntryId);
            Assert.Equal(LessonDeliveryStatus.Planned, createdNote.DeliveryStatus);

            // 2b. Supervisor reviews and approves the lesson note
            var reviewNoteReq = new ReviewLessonNoteRequest
            {
                Status = LessonNoteStatus.Approved,
                ReviewerComments = "Well structured lesson plan."
            };
            var approvedNote = await _lessonService.ReviewLessonNote(createdNote.Id, reviewNoteReq, supervisorPrincipal);
            Assert.Equal(LessonNoteStatus.Approved, approvedNote.Status);

            // 3. Mark delivery
            var deliveryReq = new MarkLessonDeliveryRequest
            {
                DeliveryStatus = LessonDeliveryStatus.Delivered,
                DeliveryRemark = "All learners completed group exercises successfully."
            };

            var updatedNote = await _lessonService.MarkLessonDelivery(createdNote.Id, deliveryReq, teacher1Principal);
            Assert.Equal(LessonDeliveryStatus.Delivered, updatedNote.DeliveryStatus);
            Assert.NotNull(updatedNote.DeliveredAt);

            // 4. Verify scheme progress reflects delivery
            var schemes = await _schemeService.GetSchemeOfWork(sectionA.Id, term.Id, null, supervisorPrincipal);
            var parentScheme = schemes.First(s => s.Id == scheme.Id);
            Assert.Equal(1, parentScheme.DeliveredLessonCount);
            Assert.Equal(1, parentScheme.TotalLessonCount);
        }

        [Fact]
        public async Task Parent_Or_Student_Cannot_Read_Unapproved_Lesson_Notes()
        {
            var (term, sectionA, _, teacher1, _, _, _) = await SeedBaseDataAsync();
            var parentPrincipal = CreatePrincipal("user-parent-a", "parent", "Parent Alice");

            var unapprovedNote = new LessonNote
            {
                Id = Guid.NewGuid(),
                NoteCode = "LN-001",
                StaffId = teacher1.Id,
                ClassSectionId = sectionA.Id,
                AcademicTermId = term.Id,
                Topic = "Secret Curriculum Draft",
                Status = LessonNoteStatus.Submitted, // Not Approved
                DeliveryStatus = LessonDeliveryStatus.Planned,
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            var approvedNote = new LessonNote
            {
                Id = Guid.NewGuid(),
                NoteCode = "LN-002",
                StaffId = teacher1.Id,
                ClassSectionId = sectionA.Id,
                AcademicTermId = term.Id,
                Topic = "Public Published Lesson",
                Status = LessonNoteStatus.Approved,
                DeliveryStatus = LessonDeliveryStatus.Planned,
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _context.LessonNotes.AddRangeAsync(unapprovedNote, approvedNote);
            await _context.SaveChangesAsync();

            var visibleNotes = (await _lessonService.GetLessonNotes(null, sectionA.Id, null, parentPrincipal)).ToList();

            Assert.Single(visibleNotes);
            Assert.Equal(approvedNote.Id, visibleNotes[0].Id);
        }

        [Fact]
        public async Task Teacher_Can_Update_Timetable_For_Assigned_Class_And_Not_Other_Class()
        {
            var (term, sectionA, sectionB, teacher1, _, _, _) = await SeedBaseDataAsync();
            var teacher1Principal = CreatePrincipal("user-teacher-1", "teacher", "Teacher One");

            var timetableReq = new UpdateTimetableRequest
            {
                Slots = new List<TimetableSlotRequest>
                {
                    new TimetableSlotRequest
                    {
                        DayOfWeek = DayOfWeekEnum.Monday,
                        PeriodNumber = 1,
                        Subject = "Mathematics",
                        StartTime = TimeSpan.FromHours(8),
                        EndTime = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(45))
                    }
                }
            };

            // 1. Updating assigned class succeeds
            var success = await _schemeService.UpdateTimetable(sectionA.Id, timetableReq, teacher1Principal);
            Assert.True(success);

            // 2. Updating unassigned class fails
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _schemeService.UpdateTimetable(sectionB.Id, timetableReq, teacher1Principal));
        }

        [Fact]
        public async Task Student_Cannot_Update_Timetable_Or_Assignments()
        {
            var (term, sectionA, _, _, _, studentA, _) = await SeedBaseDataAsync();
            var studentPrincipal = CreatePrincipal("user-student-a", "student", "Student Alice");

            var timetableReq = new UpdateTimetableRequest
            {
                Slots = new List<TimetableSlotRequest>()
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _schemeService.UpdateTimetable(sectionA.Id, timetableReq, studentPrincipal));

            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                ClassSectionId = sectionA.Id,
                Title = "Math Homework 1",
                Status = AssignmentStatus.Published,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Assignments.AddAsync(assignment);
            await _context.SaveChangesAsync();

            var updateAsgReq = new UpdateAssignmentRequest
            {
                Title = "Hacked Title"
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _lessonService.UpdateAssignment(assignment.Id, updateAsgReq, studentPrincipal));
        }

        [Fact]
        public async Task Student_Cannot_Read_Lesson_Notes_Or_Curriculum_For_Another_Class()
        {
            var (term, sectionA, sectionB, teacher1, _, studentA, _) = await SeedBaseDataAsync();
            var studentPrincipal = CreatePrincipal("user-student-a", "student", "Student Alice");

            // Lesson notes query for unassigned class throws UnauthorizedAccessException
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _lessonService.GetLessonNotes(null, sectionB.Id, null, studentPrincipal));

            // Curriculum topics query for unassigned class throws UnauthorizedAccessException
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _curriculumService.GetCurriculumTopics(null, sectionB.Id, null, studentPrincipal));
        }

        [Fact]
        public async Task Parent_Cannot_Read_Lesson_Notes_For_Unlinked_Class()
        {
            var (term, sectionA, sectionB, teacher1, _, _, parentA) = await SeedBaseDataAsync();
            var parentPrincipal = CreatePrincipal("user-parent-a", "parent", "Parent Alice");

            // Parent A has ward only in section A; querying section B throws UnauthorizedAccessException
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _lessonService.GetLessonNotes(null, sectionB.Id, null, parentPrincipal));
        }

        [Fact]
        public async Task Non_Admin_Cannot_Mutate_Administrative_Resources()
        {
            var teacherPrincipal = CreatePrincipal("user-teacher-1", "teacher", "Teacher One");
            var studentPrincipal = CreatePrincipal("user-student-a", "student", "Student Alice");
            var adminPrincipal = CreatePrincipal("user-supervisor", "super_admin", "Super Admin");

            Assert.Throws<UnauthorizedAccessException>(() =>
                ClassSectionHelper.ValidateAdministrativeMutationAccess(teacherPrincipal, "create-class", "class sections"));

            Assert.Throws<UnauthorizedAccessException>(() =>
                ClassSectionHelper.ValidateAdministrativeMutationAccess(studentPrincipal, "create-term", "academic terms"));

            // Super admin passes without exception
            ClassSectionHelper.ValidateAdministrativeMutationAccess(adminPrincipal, "create-class", "class sections");
            ClassSectionHelper.ValidateAdministrativeMutationAccess(adminPrincipal, "create-term", "academic terms");
        }
    }
}
