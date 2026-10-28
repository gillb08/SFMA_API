using Microsoft.EntityFrameworkCore;
using SFMA_API.Data.Interfaces;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using SFMA_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFMA_API.Services.Implementation
{
    public class ClassSectionService : IClassSectionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ClassSectionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private static IQueryable<ClassSectionResponse> ProjectClasses(IQueryable<ClassSection> query)
        {
            return query.Select(c => new ClassSectionResponse
            {
                Id = c.Id,
                Name = c.Name,
                Division = c.Division,
                HomeroomTeacherId = c.HomeroomTeacherId,
                HomeroomTeacherName = c.HomeroomTeacher != null && c.HomeroomTeacher.User != null
                    ? c.HomeroomTeacher.User.DisplayName
                    : null,
                AcademicTermId = c.AcademicTermId,
                AcademicTermName = c.AcademicTerm != null
                    ? (c.AcademicTerm.SessionName + " Term " + c.AcademicTerm.TermNumber)
                    : null,
                StudentCount = c.Students.Count
            });
        }

        public async Task<IEnumerable<ClassSectionResponse>> GetAllClasses(Guid? termId = null)
        {
            var query = _unitOfWork.GetRepository<ClassSection>().GetQueryable();

            if (termId.HasValue && termId.Value != Guid.Empty)
            {
                query = query.Where(c => c.AcademicTermId == termId.Value);
            }

            return await ProjectClasses(query.OrderBy(c => c.Name)).ToListAsync();
        }

        public async Task<ClassSectionResponse?> GetClassById(Guid id)
        {
            return await ProjectClasses(
                _unitOfWork.GetRepository<ClassSection>().GetQueryable(c => c.Id == id))
                .FirstOrDefaultAsync();
        }

        public async Task<ClassSectionResponse> CreateClass(CreateClassSectionRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            string normalizedName = request.Name.Trim();
            string normalizedDivision = request.Division.Trim();
            var classRepo = _unitOfWork.GetRepository<ClassSection>();

            Guid termId;
            if (request.AcademicTermId.HasValue && request.AcademicTermId.Value != Guid.Empty)
            {
                termId = request.AcademicTermId.Value;
            }
            else
            {
                var activeTerm = await _unitOfWork.GetRepository<AcademicTerm>()
                    .GetSingleByAsync(t => t.IsActive && t.Active)
                    ?? await _unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => t.Active);

                if (activeTerm == null)
                {
                    throw new InvalidOperationException("No academic term is configured. Please create an academic term first.");
                }
                termId = activeTerm.Id;
            }

            var existing = await classRepo.GetSingleByAsync(
                c => c.Name.ToLower() == normalizedName.ToLower() && c.AcademicTermId == termId);

            if (existing != null)
            {
                throw new InvalidOperationException($"A class named '{normalizedName}' already exists in this academic term.");
            }

            if (request.HomeroomTeacherId.HasValue && request.HomeroomTeacherId.Value != Guid.Empty)
            {
                var existingClassWithTeacher = await classRepo.GetSingleByAsync(c => c.HomeroomTeacherId == request.HomeroomTeacherId.Value);
                if (existingClassWithTeacher != null)
                {
                    existingClassWithTeacher.HomeroomTeacherId = null;
                    existingClassWithTeacher.UpdatedAt = DateTime.UtcNow;
                    await classRepo.UpdateAsync(existingClassWithTeacher);
                }
            }

            var newSection = new ClassSection
            {
                Id = Guid.NewGuid(),
                Name = normalizedName,
                Division = normalizedDivision,
                HomeroomTeacherId = request.HomeroomTeacherId,
                AcademicTermId = termId,
                Active = true,
                CreatedAt = DateTime.UtcNow
            };

            await classRepo.AddAsync(newSection);
            await _unitOfWork.SaveChangesAsync();

            return (await GetClassById(newSection.Id))!;
        }

        public async Task<ClassSectionResponse> UpdateClass(Guid id, UpdateClassSectionRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var classRepo = _unitOfWork.GetRepository<ClassSection>();
            var section = await classRepo.GetByIdAsync(id);
            if (section == null)
            {
                throw new KeyNotFoundException($"Class section with ID '{id}' was not found.");
            }

            string normalizedName = request.Name.Trim();
            string normalizedDivision = request.Division.Trim();
            Guid termId = request.AcademicTermId.HasValue && request.AcademicTermId.Value != Guid.Empty
                ? request.AcademicTermId.Value
                : section.AcademicTermId;

            var collision = await classRepo.GetSingleByAsync(
                c => c.Id != id && c.Name.ToLower() == normalizedName.ToLower() && c.AcademicTermId == termId);

            if (collision != null)
            {
                throw new InvalidOperationException($"Another class named '{normalizedName}' already exists in this academic term.");
            }

            if (request.HomeroomTeacherId.HasValue && request.HomeroomTeacherId.Value != Guid.Empty && request.HomeroomTeacherId != section.HomeroomTeacherId)
            {
                var otherClass = await classRepo.GetSingleByAsync(c => c.Id != id && c.HomeroomTeacherId == request.HomeroomTeacherId.Value);
                if (otherClass != null)
                {
                    otherClass.HomeroomTeacherId = null;
                    otherClass.UpdatedAt = DateTime.UtcNow;
                    await classRepo.UpdateAsync(otherClass);
                }
            }

            section.Name = normalizedName;
            section.Division = normalizedDivision;
            section.HomeroomTeacherId = request.HomeroomTeacherId;
            section.AcademicTermId = termId;
            section.UpdatedAt = DateTime.UtcNow;

            await classRepo.UpdateAsync(section);
            await _unitOfWork.SaveChangesAsync();

            return (await GetClassById(section.Id))!;
        }

        public async Task<bool> DeleteClass(Guid id)
        {
            var classRepo = _unitOfWork.GetRepository<ClassSection>();
            var section = await classRepo.GetByIdAsync(id);

            if (section == null)
            {
                throw new KeyNotFoundException($"Class section with ID '{id}' was not found.");
            }

            long enrolledCount = await _unitOfWork.GetRepository<Student>().CountAsync(s => s.ClassSectionId == id);
            if (enrolledCount > 0)
            {
                throw new InvalidOperationException($"Cannot delete '{section.Name}' because it currently has {enrolledCount} enrolled student(s). Reassign or transfer students first.");
            }

            await classRepo.DeleteAsync(section);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<string>> GetDivisions()
        {
            return await _unitOfWork.GetRepository<ClassSection>().GetQueryable()
                .Where(c => !string.IsNullOrWhiteSpace(c.Division))
                .Select(c => c.Division)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
        }
    }
}
