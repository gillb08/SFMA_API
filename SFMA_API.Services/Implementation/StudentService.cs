using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SFMA_API.Data.Interfaces;
using SFMA_API.Models.Configuration;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using SFMA_API.Models.Enums;
using SFMA_API.Services.Exceptions;
using SFMA_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SFMA_API.Services.Implementation
{
    public class StudentService : IStudentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly SchoolSettings _schoolSettings;
        private readonly ISchoolNotificationService _notificationService;

        public StudentService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IMapper mapper,
            IOptions<SchoolSettings> schoolSettings,
            ISchoolNotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _mapper = mapper;
            _schoolSettings = schoolSettings.Value;
            _notificationService = notificationService;
        }

        private static IQueryable<StudentResponse> ProjectStudents(IQueryable<Student> query)
        {
            return query.Select(s => new StudentResponse
            {
                Id = s.Id,
                UserId = s.UserId,
                StudentCode = s.StudentCode,
                FullName = s.FullName,
                Gender = s.Gender.ToString(),
                DateOfBirth = s.DateOfBirth,
                ClassSectionId = s.ClassSectionId,
                ClassSectionName = s.ClassSection != null ? s.ClassSection.Name : string.Empty,
                ClassTeacherName = s.ClassSection != null && s.ClassSection.HomeroomTeacher != null && s.ClassSection.HomeroomTeacher.User != null
                    ? s.ClassSection.HomeroomTeacher.User.DisplayName
                    : null,
                AcademicSessionName = s.ClassSection != null && s.ClassSection.AcademicTerm != null
                    ? s.ClassSection.AcademicTerm.SessionName
                    : null,
                PhotoUrl = s.PhotoUrl,
                Status = s.Status,
                AdmittedAt = s.AdmittedAt,
                IdCard = s.IdCard != null
                    ? new IdCardResponse
                    {
                        Id = s.IdCard.Id,
                        StudentId = s.Id,
                        StudentName = s.FullName,
                        StudentCode = s.StudentCode,
                        ClassSectionName = s.ClassSection != null ? s.ClassSection.Name : string.Empty,
                        ClassTeacherName = s.ClassSection != null && s.ClassSection.HomeroomTeacher != null && s.ClassSection.HomeroomTeacher.User != null
                            ? s.ClassSection.HomeroomTeacher.User.DisplayName
                            : null,
                        AcademicSessionName = s.ClassSection != null && s.ClassSection.AcademicTerm != null
                            ? s.ClassSection.AcademicTerm.SessionName
                            : null,
                        PhotoUrl = s.PhotoUrl,
                        CardSerial = s.IdCard.CardSerial,
                        GeneratedAt = s.IdCard.GeneratedAt,
                        IssuedAt = s.IdCard.GeneratedAt,
                        ExpiresAt = s.IdCard.GeneratedAt.AddYears(1),
                        NfcProvisioned = s.IdCard.NfcProvisioned,
                        Status = s.IdCard.Status,
                        EmergencyContactName = s.StudentParents.Where(sp => sp.IsPrimary).Select(sp => sp.Parent.FullName).FirstOrDefault()
                            ?? s.StudentParents.Select(sp => sp.Parent.FullName).FirstOrDefault(),
                        EmergencyContactPhone = s.StudentParents.Where(sp => sp.IsPrimary).Select(sp => sp.Parent.Phone).FirstOrDefault()
                            ?? s.StudentParents.Select(sp => sp.Parent.Phone).FirstOrDefault()
                    }
                    : null,
                Parents = s.StudentParents.Select(sp => new StudentParentResponse
                {
                    ParentId = sp.ParentId,
                    FullName = sp.Parent != null ? sp.Parent.FullName : string.Empty,
                    Phone = sp.Parent != null ? sp.Parent.Phone : string.Empty,
                    Email = sp.Parent != null ? sp.Parent.Email : string.Empty,
                    Relationship = sp.Relationship,
                    IsPrimary = sp.IsPrimary
                }).ToList()
            });
        }

        public async Task<PagedResponse<StudentResponse>> GetAllStudents(Guid? classId, StudentStatus? status, RequestParameters parameters, ClaimsPrincipal currentUser)
        {
            var query = _unitOfWork.GetRepository<Student>().GetQueryable();

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            if (roles.Contains("teacher") && !roles.Contains("super_admin") && !roles.Contains("academic_admin") && !roles.Contains("academic_head"))
            {
                var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId);
                if (staff != null)
                {
                    var taughtClassIds = await _unitOfWork.GetRepository<ClassSection>().GetQueryable(c => c.HomeroomTeacherId == staff.Id).Select(c => c.Id).ToListAsync();
                    query = query.Where(s => taughtClassIds.Contains(s.ClassSectionId));
                }
            }
            else if (roles.Contains("parent") && !roles.Contains("super_admin") && !roles.Contains("academic_admin"))
            {
                var parent = await _unitOfWork.GetRepository<Parent>().GetSingleByAsync(p => p.UserId == userId);
                if (parent != null)
                {
                    var childIds = await _unitOfWork.GetRepository<StudentParent>().GetQueryable(sp => sp.ParentId == parent.Id).Select(sp => sp.StudentId).ToListAsync();
                    query = query.Where(s => childIds.Contains(s.Id));
                }
            }
            else if (roles.Contains("student") && !roles.Contains("super_admin") && !roles.Contains("academic_admin"))
            {
                query = query.Where(s => s.UserId == userId);
            }

            if (classId.HasValue) query = query.Where(s => s.ClassSectionId == classId.Value);
            if (status.HasValue) query = query.Where(s => s.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var term = parameters.SearchTerm.Trim().ToLower();
                query = query.Where(s => s.FullName.ToLower().Contains(term) || s.StudentCode.ToLower().Contains(term));
            }

            var count = await query.CountAsync();
            var skip = (parameters.PageNumber - 1) * parameters.PageSize;
            var items = await ProjectStudents(query.OrderBy(s => s.StudentCode).Skip(skip).Take(parameters.PageSize)).ToListAsync();

            return new PagedResponse<StudentResponse>
            {
                MetaData = new MetaData
                {
                    TotalCount = count,
                    CurrentPage = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    TotalPages = (int)Math.Ceiling(count / (double)parameters.PageSize)
                },
                Items = items
            };
        }

        public async Task<StudentResponse?> GetStudentById(Guid id, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateStudentOwnershipAsync(_unitOfWork, currentUser, id);

            return await ProjectStudents(_unitOfWork.GetRepository<Student>().GetQueryable(s => s.Id == id))
                .FirstOrDefaultAsync();
        }

        public async Task<AdmitStudentResponse> AdmitStudent(AdmitStudentRequest request)
        {
            var studentRepo = _unitOfWork.GetRepository<Student>();
            var parentRepo = _unitOfWork.GetRepository<Parent>();

            // Duplicate Detection Validation
            string normalizedName = request.FullName.Trim().ToLower();
            var existingByNameDob = await studentRepo.GetSingleByAsync(s => s.FullName.ToLower() == normalizedName && s.DateOfBirth.Date == request.Dob.Date);
            if (existingByNameDob != null)
            {
                throw new DuplicateRecordException("full_name_and_dob", new { id = existingByNameDob.Id, name = existingByNameDob.FullName });
            }

            string normalizedPhone = new string(request.GuardianPhone.Where(char.IsDigit).ToArray());
            var existingByPhone = await parentRepo.GetSingleByAsync(p => p.Phone.Replace(" ", "").Replace("-", "") == normalizedPhone);
            if (existingByPhone != null)
            {
                var existingChild = await _unitOfWork.GetRepository<StudentParent>().GetSingleByAsync(sp => sp.ParentId == existingByPhone.Id, include: q => q.Include(sp => sp.Student));
                throw new DuplicateRecordException("guardian_phone", existingChild != null ? new { id = existingChild.StudentId, name = existingChild.Student.FullName } : null);
            }

            if (!string.IsNullOrWhiteSpace(request.GuardianEmail))
            {
                var existingByEmail = await parentRepo.GetSingleByAsync(p => p.Email.ToLower() == request.GuardianEmail.Trim().ToLower());
                if (existingByEmail != null)
                {
                    var existingChild = await _unitOfWork.GetRepository<StudentParent>().GetSingleByAsync(sp => sp.ParentId == existingByEmail.Id, include: q => q.Include(sp => sp.Student));
                    throw new DuplicateRecordException("guardian_email", existingChild != null ? new { id = existingChild.StudentId, name = existingChild.Student.FullName } : null);
                }
            }

            // Generate Student Code — year is dynamic, prefix comes from config
            var totalStudents = await studentRepo.CountAsync();
            int currentYear = DateTime.UtcNow.Year;
            string prefix = _schoolSettings.StudentCodePrefix;
            string studentCode = $"{prefix}-{currentYear}-{(totalStudents + 1):D4}";

            // Email domain comes from configuration — not hardcoded
            string emailDomain = _schoolSettings.EmailDomain;
            string studentEmail = $"{studentCode.ToLower()}@{emailDomain}";

            // Create Student User
            var studentUser = new ApplicationUser
            {
                DisplayName = request.FullName,
                Email = studentEmail,
                UserName = studentCode,
                Status = UserStatus.Active,
                Active = true,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(studentUser, "Student@123!");
            await _userManager.AddToRoleAsync(studentUser, "student");

            // Create Parent User & Record
            string parentEmail = !string.IsNullOrWhiteSpace(request.GuardianEmail)
                ? request.GuardianEmail.Trim().ToLower()
                : $"parent.{studentCode.ToLower()}@{emailDomain}";

            var parentUser = new ApplicationUser
            {
                DisplayName = request.GuardianName,
                Email = parentEmail,
                UserName = parentEmail,
                PhoneNumber = request.GuardianPhone,
                Status = UserStatus.Active,
                Active = true,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(parentUser, "Parent@123!");
            await _userManager.AddToRoleAsync(parentUser, "parent");

            var parent = new Parent
            {
                Id = Guid.NewGuid(),
                UserId = parentUser.Id,
                FullName = request.GuardianName,
                Phone = request.GuardianPhone,
                Email = parentEmail,
                Address = request.Address
            };
            await parentRepo.AddAsync(parent);

            // Ensure target ClassSection exists
            var classSection = await ClassSectionHelper.EnsureClassSectionAsync(_unitOfWork, request.ClassAdmitted, request.ClassName);

            // Parse Gender
            Enum.TryParse<Gender>(request.Gender, true, out var parsedGender);

            // Create Student
            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = studentUser.Id,
                StudentCode = studentCode,
                FullName = request.FullName,
                Gender = parsedGender,
                DateOfBirth = request.Dob,
                ClassSectionId = classSection.Id,
                Status = StudentStatus.Admitted,
                AdmittedAt = DateTime.UtcNow
            };
            await studentRepo.AddAsync(student);

            // Link Student-Parent — relationship comes from request, not hardcoded
            var studentParent = new StudentParent
            {
                StudentId = student.Id,
                ParentId = parent.Id,
                Relationship = request.GuardianRelationship,
                IsPrimary = true
            };
            await _unitOfWork.GetRepository<StudentParent>().AddAsync(studentParent);

            // Create ID Card — year is dynamic; use Random.Shared to avoid collision risk
            string cardSerial = $"{prefix}-NFC-{Random.Shared.Next(10000, 99999)}-{currentYear}";
            var idCard = new IdCard
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                CardSerial = cardSerial,
                GeneratedAt = DateTime.UtcNow,
                NfcProvisioned = true,
                Status = IdCardStatus.Generated
            };
            await _unitOfWork.GetRepository<IdCard>().AddAsync(idCard);

            await _unitOfWork.SaveChangesAsync();

            var studentResponse = _mapper.Map<StudentResponse>(student);
            studentResponse.ClassSectionName = classSection.Name;
            var parentAcc = _mapper.Map<UserProfileResponse>(parentUser);
            parentAcc.Role = "parent";

            var studentAcc = _mapper.Map<UserProfileResponse>(studentUser);
            studentAcc.Role = "student";
            studentAcc.AssignedClassId = classSection.Id;
            studentAcc.AssignedClassName = classSection.Name;

            var cardResponse = _mapper.Map<IdCardResponse>(idCard);
            cardResponse.StudentName = student.FullName;
            cardResponse.StudentCode = student.StudentCode;
            cardResponse.ClassSectionName = classSection.Name;
            cardResponse.IssuedAt = idCard.GeneratedAt;
            cardResponse.ExpiresAt = idCard.GeneratedAt.AddYears(1);

            string className = classSection.Name;

            // Fire and forget / background dispatch notification to parent & scholar
            _ = Task.Run(() => _notificationService.SendParentAdmissionCredentialsAsync(
                request.GuardianName,
                parentEmail,
                request.GuardianPhone,
                request.FullName,
                studentCode,
                className,
                "Student@123!",
                "Parent@123!"));

            return new AdmitStudentResponse
            {
                Student = studentResponse,
                ParentAccount = parentAcc,
                StudentAccount = studentAcc,
                IdCard = cardResponse
            };
        }

        public async Task<StudentResponse> UpdateBioData(Guid id, UpdateStudentBioDataRequest request)
        {
            var studentRepo = _unitOfWork.GetRepository<Student>();
            var student = await studentRepo.GetByIdAsync(id);
            if (student == null)
            {
                throw new KeyNotFoundException("Student not found");
            }

            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                student.FullName = request.FullName.Trim();
                var studentUser = await _userManager.FindByIdAsync(student.UserId);
                if (studentUser != null)
                {
                    studentUser.DisplayName = request.FullName.Trim();
                    studentUser.UpdatedAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(studentUser);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Gender) && Enum.TryParse<Gender>(request.Gender, true, out var gender))
            {
                student.Gender = gender;
            }

            if (request.DateOfBirth.HasValue)
            {
                student.DateOfBirth = request.DateOfBirth.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.PhotoUrl))
            {
                student.PhotoUrl = request.PhotoUrl;
            }

            if (request.ClassSectionId.HasValue && request.ClassSectionId.Value != Guid.Empty)
            {
                var targetClass = await ClassSectionHelper.EnsureClassSectionAsync(_unitOfWork, request.ClassSectionId.Value, request.ClassName);
                student.ClassSectionId = targetClass.Id;
            }

            var primaryParentId = await _unitOfWork.GetRepository<StudentParent>()
                .GetQueryable(sp => sp.StudentId == id)
                .OrderByDescending(sp => sp.IsPrimary)
                .Select(sp => (Guid?)sp.ParentId)
                .FirstOrDefaultAsync();

            if (primaryParentId.HasValue)
            {
                var parentRepo = _unitOfWork.GetRepository<Parent>();
                var primaryParent = await parentRepo.GetByIdAsync(primaryParentId.Value);
                if (primaryParent != null)
                {
                    bool parentUpdated = false;
                    if (!string.IsNullOrWhiteSpace(request.GuardianName))
                    {
                        primaryParent.FullName = request.GuardianName.Trim();
                        parentUpdated = true;
                    }
                    if (!string.IsNullOrWhiteSpace(request.GuardianPhone))
                    {
                        primaryParent.Phone = request.GuardianPhone.Trim();
                        parentUpdated = true;
                    }
                    if (!string.IsNullOrWhiteSpace(request.Address))
                    {
                        primaryParent.Address = request.Address.Trim();
                        parentUpdated = true;
                    }
                    if (parentUpdated)
                    {
                        primaryParent.UpdatedAt = DateTime.UtcNow;
                        await parentRepo.UpdateAsync(primaryParent);
                    }
                }
            }

            student.UpdatedAt = DateTime.UtcNow;
            await studentRepo.UpdateAsync(student);
            await _unitOfWork.SaveChangesAsync();

            return (await ProjectStudents(studentRepo.GetQueryable(s => s.Id == id)).FirstOrDefaultAsync())!;
        }

        public async Task<IdCardResponse?> GetIdCard(Guid studentId, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateStudentOwnershipAsync(_unitOfWork, currentUser, studentId);

            return await _unitOfWork.GetRepository<IdCard>().GetQueryable(c => c.StudentId == studentId)
                .Select(c => new IdCardResponse
                {
                    Id = c.Id,
                    StudentId = c.StudentId,
                    StudentName = c.Student != null ? c.Student.FullName : string.Empty,
                    StudentCode = c.Student != null ? c.Student.StudentCode : string.Empty,
                    ClassSectionName = c.Student != null && c.Student.ClassSection != null ? c.Student.ClassSection.Name : string.Empty,
                    ClassTeacherName = c.Student != null && c.Student.ClassSection != null && c.Student.ClassSection.HomeroomTeacher != null && c.Student.ClassSection.HomeroomTeacher.User != null
                        ? c.Student.ClassSection.HomeroomTeacher.User.DisplayName
                        : null,
                    AcademicSessionName = c.Student != null && c.Student.ClassSection != null && c.Student.ClassSection.AcademicTerm != null
                        ? c.Student.ClassSection.AcademicTerm.SessionName
                        : null,
                    PhotoUrl = c.Student != null ? c.Student.PhotoUrl : null,
                    CardSerial = c.CardSerial,
                    GeneratedAt = c.GeneratedAt,
                    IssuedAt = c.GeneratedAt,
                    ExpiresAt = c.GeneratedAt.AddYears(1),
                    NfcProvisioned = c.NfcProvisioned,
                    Status = c.Status,
                    EmergencyContactName = c.Student != null
                        ? (c.Student.StudentParents.Where(sp => sp.IsPrimary).Select(sp => sp.Parent.FullName).FirstOrDefault()
                            ?? c.Student.StudentParents.Select(sp => sp.Parent.FullName).FirstOrDefault())
                        : null,
                    EmergencyContactPhone = c.Student != null
                        ? (c.Student.StudentParents.Where(sp => sp.IsPrimary).Select(sp => sp.Parent.Phone).FirstOrDefault()
                            ?? c.Student.StudentParents.Select(sp => sp.Parent.Phone).FirstOrDefault())
                        : null
                })
                .FirstOrDefaultAsync();
        }

        public async Task<byte[]> BatchPrint(BatchPrintRequest request)
        {
            var students = await _unitOfWork.GetRepository<Student>().GetByAsync(
                s => request.StudentIds.Contains(s.Id),
                include: q => q.Include(s => s.ClassSection));

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                foreach (var student in students)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A6.Landscape());
                        page.Margin(15);
                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("ST. FAITH MODEL ACADEMY").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                                col.Item().Text("OFFICIAL STUDENT IDENTIFICATION CARD").FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                        });

                        page.Content().PaddingVertical(10).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Student Name: {student.FullName}").Bold().FontSize(11);
                                col.Item().Text($"Student Code: {student.StudentCode}").FontSize(9);
                                col.Item().Text($"Class Section: {student.ClassSection?.Name ?? "General"}").FontSize(9);
                                col.Item().Text($"Admitted Date: {student.AdmittedAt:dd MMM yyyy}").FontSize(9);
                                col.Item().Text("Emergency Contact: School Administration Office").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        });

                        page.Footer().AlignCenter().Text(t =>
                        {
                            t.Span("St. Faith Model Academy • Excellence & Integrity • Valid for Active Session").FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                    });
                }
            });

            return document.GeneratePdf();
        }
    }
}
