using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SFMA_API.Data.Interfaces;
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

namespace SFMA_API.Services.Implementation
{
    public class StudentService : IStudentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public StudentService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<PagedResponse<StudentResponse>> GetAllStudents(Guid? classId, StudentStatus? status, RequestParameters parameters, ClaimsPrincipal currentUser)
        {
            var query = _unitOfWork.GetRepository<Student>().GetQueryable(
                include: q => q.Include(s => s.ClassSection)
                               .Include(s => s.StudentParents).ThenInclude(sp => sp.Parent));

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            if (roles.Contains("teacher") && !roles.Contains("super_admin") && !roles.Contains("academic_admin"))
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
            var items = await query.OrderBy(s => s.StudentCode).Skip(skip).Take(parameters.PageSize).ToListAsync();

            return new PagedResponse<StudentResponse>
            {
                MetaData = new MetaData
                {
                    TotalCount = count,
                    CurrentPage = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    TotalPages = (int)Math.Ceiling(count / (double)parameters.PageSize)
                },
                Items = _mapper.Map<IEnumerable<StudentResponse>>(items)
            };
        }

        public async Task<StudentResponse?> GetStudentById(Guid id, ClaimsPrincipal currentUser)
        {
            var student = await _unitOfWork.GetRepository<Student>().GetSingleByAsync(
                s => s.Id == id,
                include: q => q.Include(s => s.ClassSection)
                               .Include(s => s.StudentParents).ThenInclude(sp => sp.Parent));

            if (student == null) return null;

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            if (roles.Contains("student") && student.UserId != userId && !roles.Contains("super_admin") && !roles.Contains("academic_admin"))
            {
                throw new UnauthorizedAccessException("You can only view your own student profile");
            }

            if (roles.Contains("parent") && !roles.Contains("super_admin") && !roles.Contains("academic_admin"))
            {
                var parent = await _unitOfWork.GetRepository<Parent>().GetSingleByAsync(p => p.UserId == userId);
                bool isChild = parent != null && student.StudentParents.Any(sp => sp.ParentId == parent.Id);
                if (!isChild)
                {
                    throw new UnauthorizedAccessException("You can only view your own child's profile");
                }
            }

            return _mapper.Map<StudentResponse>(student);
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

            // Generate Student Code
            var totalStudents = await studentRepo.CountAsync();
            string studentCode = $"SF-2026-{(totalStudents + 1):D4}";
            string studentEmail = $"{studentCode.ToLower()}@stfaithacademy.edu.ng";

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
                : $"parent.{studentCode.ToLower()}@stfaithacademy.edu.ng";

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
                ClassSectionId = request.ClassAdmitted,
                Status = StudentStatus.Admitted,
                AdmittedAt = DateTime.UtcNow
            };
            await studentRepo.AddAsync(student);

            // Link Student-Parent
            var studentParent = new StudentParent
            {
                StudentId = student.Id,
                ParentId = parent.Id,
                Relationship = "Guardian",
                IsPrimary = true
            };
            await _unitOfWork.GetRepository<StudentParent>().AddAsync(studentParent);

            // Create ID Card
            string cardSerial = $"SF-NFC-{(new Random().Next(10000, 99999))}-2026";
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
            var parentAcc = _mapper.Map<UserProfileResponse>(parentUser);
            parentAcc.Role = "parent";

            var studentAcc = _mapper.Map<UserProfileResponse>(studentUser);
            studentAcc.Role = "student";

            var cardResponse = _mapper.Map<IdCardResponse>(idCard);
            cardResponse.StudentName = student.FullName;
            cardResponse.StudentCode = student.StudentCode;

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
            var student = await studentRepo.GetSingleByAsync(s => s.Id == id, include: q => q.Include(s => s.User).Include(s => s.StudentParents).ThenInclude(sp => sp.Parent));
            if (student == null)
            {
                throw new KeyNotFoundException("Student not found");
            }

            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                student.FullName = request.FullName;
                student.User.DisplayName = request.FullName;
                await _userManager.UpdateAsync(student.User);
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

            student.UpdatedAt = DateTime.UtcNow;
            await studentRepo.UpdateAsync(student);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<StudentResponse>(student);
        }

        public async Task<IdCardResponse?> GetIdCard(Guid studentId, ClaimsPrincipal currentUser)
        {
            var idCard = await _unitOfWork.GetRepository<IdCard>().GetSingleByAsync(
                c => c.StudentId == studentId,
                include: q => q.Include(c => c.Student));

            return idCard != null ? _mapper.Map<IdCardResponse>(idCard) : null;
        }

        public async Task<byte[]> BatchPrint(BatchPrintRequest request)
        {
            var students = await _unitOfWork.GetRepository<Student>().GetByAsync(
                s => request.StudentIds.Contains(s.Id),
                include: q => q.Include(s => s.ClassSection));

            var sb = new StringBuilder();
            sb.AppendLine("Student Code,Full Name,Class,Admitted Date");
            foreach (var s in students)
            {
                sb.AppendLine($"{s.StudentCode},{s.FullName},{s.ClassSection?.Name ?? "N/A"},{s.AdmittedAt:yyyy-MM-dd}");
            }
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
