using Microsoft.EntityFrameworkCore;
using SFMA_API.Data.Interfaces;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using SFMA_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SFMA_API.Services.Implementation
{
    public class CurriculumTopicService : ICurriculumTopicService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CurriculumTopicService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<CurriculumTopicResponse>> GetCurriculumTopics(Guid? subjectId, Guid? classSectionId, Guid? academicTermId, ClaimsPrincipal currentUser)
        {
            var query = _unitOfWork.GetRepository<CurriculumTopic>().GetQueryable(c => c.Active);

            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSupervisor = roles.Contains("super_admin") || roles.Contains("academic_admin") || roles.Contains("academic_head") || roles.Contains("admin");

            if (!isSupervisor)
            {
                string? currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (roles.Contains("teacher"))
                {
                    var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == currentUserId);
                    var assignedClass = staff != null ? await _unitOfWork.GetRepository<ClassSection>().GetSingleByAsync(c => c.HomeroomTeacherId == staff.Id) : null;
                    if (assignedClass != null)
                    {
                        if (classSectionId.HasValue && classSectionId.Value != Guid.Empty && classSectionId.Value != assignedClass.Id)
                        {
                            throw new UnauthorizedAccessException($"Access denied: You are assigned solely to {assignedClass.Name} and cannot view curriculum topics for another class.");
                        }
                        if (!classSectionId.HasValue || classSectionId.Value == Guid.Empty)
                        {
                            query = query.Where(c => c.ClassSectionId == null || c.ClassSectionId == assignedClass.Id);
                        }
                    }
                }
                else if (roles.Contains("student"))
                {
                    var student = await _unitOfWork.GetRepository<Student>().GetSingleByAsync(s => s.UserId == currentUserId);
                    if (student == null) return Enumerable.Empty<CurriculumTopicResponse>();

                    if (classSectionId.HasValue && classSectionId.Value != Guid.Empty && classSectionId.Value != student.ClassSectionId)
                    {
                        throw new UnauthorizedAccessException("Access denied: Students may only view curriculum topics for their assigned class.");
                    }
                    query = query.Where(c => c.ClassSectionId == null || c.ClassSectionId == student.ClassSectionId);
                }
                else if (roles.Contains("parent"))
                {
                    var parent = await _unitOfWork.GetRepository<Parent>().GetSingleByAsync(
                        p => p.UserId == currentUserId,
                        include: q => q.Include(p => p.StudentParents).ThenInclude(sp => sp.Student));

                    if (parent == null || !parent.StudentParents.Any(sp => sp.Student != null))
                    {
                        return Enumerable.Empty<CurriculumTopicResponse>();
                    }

                    var allowedClassIds = parent.StudentParents
                        .Where(sp => sp.Student != null)
                        .Select(sp => sp.Student!.ClassSectionId)
                        .Distinct()
                        .ToList();

                    if (classSectionId.HasValue && classSectionId.Value != Guid.Empty)
                    {
                        if (!allowedClassIds.Contains(classSectionId.Value))
                        {
                            throw new UnauthorizedAccessException("Access denied: Parents may only view curriculum topics for classes containing their linked wards.");
                        }
                        query = query.Where(c => c.ClassSectionId == null || c.ClassSectionId == classSectionId.Value);
                    }
                    else
                    {
                        query = query.Where(c => c.ClassSectionId == null || allowedClassIds.Contains(c.ClassSectionId.Value));
                    }
                }
                else
                {
                    throw new UnauthorizedAccessException("Access denied: Insufficient permissions to access curriculum topics.");
                }
            }

            if (subjectId.HasValue && subjectId.Value != Guid.Empty)
            {
                query = query.Where(c => c.SubjectId == subjectId.Value);
            }

            if (classSectionId.HasValue && classSectionId.Value != Guid.Empty)
            {
                query = query.Where(c => c.ClassSectionId == null || c.ClassSectionId == classSectionId.Value);
            }

            if (academicTermId.HasValue && academicTermId.Value != Guid.Empty)
            {
                query = query.Where(c => c.AcademicTermId == academicTermId.Value);
            }

            return await query
                .OrderBy(c => c.SequenceNumber)
                .ThenBy(c => c.Topic)
                .Select(c => new CurriculumTopicResponse
                {
                    Id = c.Id,
                    SubjectId = c.SubjectId,
                    SubjectName = c.Subject != null ? c.Subject.Name : string.Empty,
                    ClassSectionId = c.ClassSectionId,
                    ClassSectionName = c.ClassSection != null ? c.ClassSection.Name : null,
                    AcademicTermId = c.AcademicTermId,
                    AcademicTermName = c.AcademicTerm != null ? $"Term {c.AcademicTerm.TermNumber} ({c.AcademicTerm.SessionName})" : string.Empty,
                    Topic = c.Topic,
                    LearningOutcomes = c.LearningOutcomes,
                    SequenceNumber = c.SequenceNumber,
                    Active = c.Active,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<CurriculumTopicResponse> CreateCurriculumTopic(CreateCurriculumTopicRequest request, ClaimsPrincipal currentUser)
        {
            ClassSectionHelper.ValidateCurriculumMutationAccess(currentUser, "create-curriculum-topic");

            if (request.SubjectId == Guid.Empty)
                throw new ArgumentException("A valid Subject identifier is required.");
            if (request.AcademicTermId == Guid.Empty)
                throw new ArgumentException("A valid Academic Term identifier is required.");
            if (string.IsNullOrWhiteSpace(request.Topic))
                throw new ArgumentException("Topic name cannot be empty.");

            var topic = new CurriculumTopic
            {
                Id = Guid.NewGuid(),
                SubjectId = request.SubjectId,
                ClassSectionId = request.ClassSectionId != Guid.Empty ? request.ClassSectionId : null,
                AcademicTermId = request.AcademicTermId,
                Topic = request.Topic.Trim(),
                LearningOutcomes = request.LearningOutcomes?.Trim(),
                SequenceNumber = request.SequenceNumber > 0 ? request.SequenceNumber : (short)1,
                Active = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<CurriculumTopic>().AddAsync(topic);

            var staffId = currentUser?.FindFirst("Id")?.Value ?? currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == staffId, include: q => q.Include(s => s.User));

            var auditLog = new AcademicWorkflowAuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "CurriculumTopic",
                EntityId = topic.Id,
                Action = "Created",
                ToStatus = "Active",
                ActorStaffId = staff?.Id,
                ActorName = staff?.User?.DisplayName ?? currentUser?.Identity?.Name ?? "Supervisor",
                ActorRole = currentUser?.FindFirst(ClaimTypes.Role)?.Value ?? "AcademicSupervisor",
                Comments = $"Created curriculum topic '{topic.Topic}'",
                Timestamp = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<AcademicWorkflowAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.GetRepository<CurriculumTopic>().GetQueryable(c => c.Id == topic.Id)
                .Select(c => new CurriculumTopicResponse
                {
                    Id = c.Id,
                    SubjectId = c.SubjectId,
                    SubjectName = c.Subject != null ? c.Subject.Name : string.Empty,
                    ClassSectionId = c.ClassSectionId,
                    ClassSectionName = c.ClassSection != null ? c.ClassSection.Name : null,
                    AcademicTermId = c.AcademicTermId,
                    AcademicTermName = c.AcademicTerm != null ? $"Term {c.AcademicTerm.TermNumber} ({c.AcademicTerm.SessionName})" : string.Empty,
                    Topic = c.Topic,
                    LearningOutcomes = c.LearningOutcomes,
                    SequenceNumber = c.SequenceNumber,
                    Active = c.Active,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();

            return created!;
        }

        public async Task<CurriculumTopicResponse> UpdateCurriculumTopic(Guid id, UpdateCurriculumTopicRequest request, ClaimsPrincipal currentUser)
        {
            ClassSectionHelper.ValidateCurriculumMutationAccess(currentUser, "update-curriculum-topic");

            var topic = await _unitOfWork.GetRepository<CurriculumTopic>().GetByIdAsync(id);
            if (topic == null)
                throw new KeyNotFoundException($"Curriculum topic with ID '{id}' was not found.");

            if (!string.IsNullOrWhiteSpace(request.Topic)) topic.Topic = request.Topic.Trim();
            if (request.LearningOutcomes != null) topic.LearningOutcomes = request.LearningOutcomes.Trim();
            if (request.SequenceNumber.HasValue) topic.SequenceNumber = request.SequenceNumber.Value;
            if (request.Active.HasValue) topic.Active = request.Active.Value;
            topic.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.GetRepository<CurriculumTopic>().Update(topic);

            var staffId = currentUser?.FindFirst("Id")?.Value ?? currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == staffId, include: q => q.Include(s => s.User));

            var auditLog = new AcademicWorkflowAuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "CurriculumTopic",
                EntityId = topic.Id,
                Action = "Updated",
                ToStatus = topic.Active ? "Active" : "Inactive",
                ActorStaffId = staff?.Id,
                ActorName = staff?.User?.DisplayName ?? currentUser?.Identity?.Name ?? "Supervisor",
                ActorRole = currentUser?.FindFirst(ClaimTypes.Role)?.Value ?? "AcademicSupervisor",
                Comments = $"Updated curriculum topic '{topic.Topic}'",
                Timestamp = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<AcademicWorkflowAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.GetRepository<CurriculumTopic>().GetQueryable(c => c.Id == topic.Id)
                .Select(c => new CurriculumTopicResponse
                {
                    Id = c.Id,
                    SubjectId = c.SubjectId,
                    SubjectName = c.Subject != null ? c.Subject.Name : string.Empty,
                    ClassSectionId = c.ClassSectionId,
                    ClassSectionName = c.ClassSection != null ? c.ClassSection.Name : null,
                    AcademicTermId = c.AcademicTermId,
                    AcademicTermName = c.AcademicTerm != null ? $"Term {c.AcademicTerm.TermNumber} ({c.AcademicTerm.SessionName})" : string.Empty,
                    Topic = c.Topic,
                    LearningOutcomes = c.LearningOutcomes,
                    SequenceNumber = c.SequenceNumber,
                    Active = c.Active,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();

            return updated!;
        }

        public async Task<bool> DeleteCurriculumTopic(Guid id, ClaimsPrincipal currentUser)
        {
            ClassSectionHelper.ValidateCurriculumMutationAccess(currentUser, "delete-curriculum-topic");

            var topic = await _unitOfWork.GetRepository<CurriculumTopic>().GetByIdAsync(id);
            if (topic == null)
                throw new KeyNotFoundException($"Curriculum topic with ID '{id}' was not found.");

            topic.Active = false;
            topic.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.GetRepository<CurriculumTopic>().Update(topic);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
