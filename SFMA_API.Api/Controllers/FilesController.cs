using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFMA_API.Data.Interfaces;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using SFMA_API.Models.Enums;
using SFMA_API.Services.Implementation;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/files")]
    [Authorize(Policy = "Authorization")]
    public class FilesController : BaseController
    {
        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg", ".csv", ".txt"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _environment;

        public FilesController(IUnitOfWork unitOfWork, IWebHostEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _environment = environment;
        }

        [HttpPost("upload", Name = "upload-file")]
        [SwaggerOperation(Summary = "Upload a document (lesson note, assignment submission, or payment teller proof)")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadFile(
            IFormFile file,
            [FromForm] string? category = "general",
            [FromForm] Guid? studentId = null,
            [FromForm] Guid? classSectionId = null)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException("Please select a valid non-empty file to upload.");
            }

            if (file.Length > 25 * 1024 * 1024)
            {
                throw new InvalidOperationException("File size exceeds the 25MB maximum upload limit.");
            }

            string safeOriginalName = Path.GetFileName(file.FileName);
            string ext = Path.GetExtension(safeOriginalName);
            if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            {
                throw new InvalidOperationException($"Unsupported file format '{ext}'. Allowed formats: PDF, DOC, DOCX, PNG, JPG, JPEG, CSV, TXT.");
            }

            string? userId = HttpContext.User.FindFirstValue("Id") ?? HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Access denied: Unable to resolve authenticated user identity.");
            }

            var roles = HttpContext.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value.ToLowerInvariant())
                .ToList();

            string normalizedCategory = (!string.IsNullOrWhiteSpace(category) ? category.Trim() : "general").ToLowerInvariant();
            Guid? resolvedStudentId = (studentId.HasValue && studentId.Value != Guid.Empty) ? studentId.Value : null;
            Guid? resolvedClassSectionId = (classSectionId.HasValue && classSectionId.Value != Guid.Empty) ? classSectionId.Value : null;

            bool isStaffOrManagement = roles.Contains("super_admin")
                || roles.Contains("academic_admin")
                || roles.Contains("academic_head")
                || roles.Contains("financial_admin")
                || roles.Contains("financial_head")
                || roles.Contains("teacher");

            if (normalizedCategory == "lesson-notes" || normalizedCategory == "scheme")
            {
                if (!isStaffOrManagement)
                {
                    throw new UnauthorizedAccessException("Access denied: Only teaching staff and academic administrators may upload lesson notes or scheme materials.");
                }

                if (resolvedClassSectionId.HasValue)
                {
                    await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, HttpContext.User, resolvedClassSectionId.Value);
                }
            }
            else if (normalizedCategory == "assignments" || normalizedCategory == "tellers" || normalizedCategory == "payment-proofs")
            {
                if (resolvedStudentId.HasValue)
                {
                    await ClassSectionHelper.ValidateStudentOwnershipAsync(_unitOfWork, HttpContext.User, resolvedStudentId.Value);
                }
                else if (roles.Contains("student"))
                {
                    var studentRecord = await _unitOfWork.GetRepository<Student>().GetQueryable()
                        .Where(s => s.UserId == userId)
                        .Select(s => new { s.Id, s.ClassSectionId })
                        .FirstOrDefaultAsync();

                    if (studentRecord == null)
                    {
                        throw new UnauthorizedAccessException("Access denied: No active student profile is linked to your account.");
                    }

                    resolvedStudentId = studentRecord.Id;
                    resolvedClassSectionId ??= studentRecord.ClassSectionId;
                }
                else if (roles.Contains("parent"))
                {
                    string? userEmail = HttpContext.User.FindFirstValue(ClaimTypes.Email);
                    var wardStudentIds = await _unitOfWork.GetRepository<StudentParent>().GetQueryable()
                        .Where(sp => sp.Parent.UserId == userId || (!string.IsNullOrEmpty(userEmail) && sp.Parent.Email != null && sp.Parent.Email.ToLower() == userEmail.ToLower()))
                        .Select(sp => sp.StudentId)
                        .ToListAsync();

                    if (!wardStudentIds.Any())
                    {
                        throw new UnauthorizedAccessException("Access denied: No linked wards were found for your parent account.");
                    }

                    if (wardStudentIds.Count == 1)
                    {
                        resolvedStudentId = wardStudentIds[0];
                    }
                }
            }

            if (resolvedClassSectionId.HasValue)
            {
                await ClassSectionHelper.ValidateClassAccessForPortalUserAsync(_unitOfWork, HttpContext.User, resolvedClassSectionId.Value);
            }

            string uploadsDir = Path.Combine(_environment.ContentRootPath, "App_Data", "Uploads");
            Directory.CreateDirectory(uploadsDir);

            Guid fileId = Guid.NewGuid();
            string storedFileName = $"{fileId:N}{ext}";
            string fullPath = Path.Combine(uploadsDir, storedFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            string resolvedContentType = !string.IsNullOrWhiteSpace(file.ContentType) ? file.ContentType : "application/octet-stream";
            var record = new UploadedFileRecord
            {
                Id = fileId,
                FileName = safeOriginalName,
                ContentType = resolvedContentType,
                FileSizeBytes = file.Length,
                StoragePath = storedFileName,
                Category = normalizedCategory,
                UploadedByUserId = userId,
                StudentId = resolvedStudentId,
                ClassSectionId = resolvedClassSectionId,
                Active = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<UploadedFileRecord>().AddAsync(record);
            await _unitOfWork.SaveChangesAsync();

            string protectedDownloadUrl = $"/api/v1/files/{record.Id}/download";
            var response = new UploadedFileResponse
            {
                Id = record.Id,
                FileName = record.FileName,
                FileType = record.ContentType,
                ContentType = record.ContentType,
                FileSizeBytes = record.FileSizeBytes,
                FileUrl = protectedDownloadUrl,
                DownloadUrl = protectedDownloadUrl,
                Category = record.Category,
                UploadedAt = record.CreatedAt
            };

            return Ok(response);
        }

        [HttpGet("{id:guid}/download", Name = "download-file")]
        [SwaggerOperation(Summary = "Download a protected uploaded file by ID")]
        public async Task<IActionResult> DownloadFile(Guid id)
        {
            var record = await _unitOfWork.GetRepository<UploadedFileRecord>().GetByIdAsync(id);
            if (record == null || !record.Active)
            {
                return NotFound();
            }

            await AuthorizeFileDownloadAsync(record, HttpContext.User);

            string uploadsDir = Path.Combine(_environment.ContentRootPath, "App_Data", "Uploads");
            string fullPath = Path.Combine(uploadsDir, record.StoragePath);

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(bytes, record.ContentType, record.FileName);
        }

        private async Task AuthorizeFileDownloadAsync(UploadedFileRecord record, ClaimsPrincipal currentUser)
        {
            string? userId = currentUser.FindFirstValue("Id") ?? currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Access denied: Authentication is required to download files.");
            }

            var roles = currentUser.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value.ToLowerInvariant())
                .ToList();

            // 1. Executive / Super Admin and Academic Admin have full audit visibility
            if (roles.Contains("super_admin") || roles.Contains("academic_admin"))
            {
                return;
            }

            // 2. Direct uploader always retains access to their own uploaded file
            if (!string.IsNullOrWhiteSpace(record.UploadedByUserId) &&
                string.Equals(record.UploadedByUserId, userId, StringComparison.Ordinal))
            {
                return;
            }

            string fileIdToken = record.Id.ToString();
            string category = (record.Category ?? "general").Trim().ToLowerInvariant();

            // 3. Check if referenced by a FeeTransaction (payment teller proof)
            var linkedTeller = await _unitOfWork.GetRepository<FeeTransaction>().GetQueryable()
                .Where(t => t.AttachmentFile != null && t.AttachmentFile.Contains(fileIdToken))
                .Select(t => new { t.Id, t.StudentId })
                .FirstOrDefaultAsync();

            if (linkedTeller != null || category == "tellers" || category == "payment-proofs")
            {
                if (roles.Contains("financial_admin") || roles.Contains("financial_head"))
                {
                    return;
                }

                // Teachers must never access student/parent financial payment proofs
                if (roles.Contains("teacher") && !roles.Contains("parent") && !roles.Contains("student"))
                {
                    throw new UnauthorizedAccessException("Access denied: Teaching staff are not authorized to access student financial payment proofs.");
                }

                Guid? targetStudentId = linkedTeller?.StudentId ?? record.StudentId;
                if (targetStudentId.HasValue && targetStudentId.Value != Guid.Empty)
                {
                    await ClassSectionHelper.ValidateStudentOwnershipAsync(_unitOfWork, currentUser, targetStudentId.Value);
                    return;
                }

                throw new UnauthorizedAccessException("Access denied: You are not authorized to view this payment proof.");
            }

            // 4. Check if referenced by an AssignmentSubmission (student homework upload)
            var linkedSubmission = await _unitOfWork.GetRepository<AssignmentSubmission>().GetQueryable()
                .Where(s => s.FileUrl != null && s.FileUrl.Contains(fileIdToken))
                .Select(s => new { s.Id, s.StudentId, ClassSectionId = s.Assignment.ClassSectionId })
                .FirstOrDefaultAsync();

            if (linkedSubmission != null || category == "assignments")
            {
                if (roles.Contains("academic_head"))
                {
                    return;
                }

                if (roles.Contains("teacher"))
                {
                    Guid? targetClassId = linkedSubmission?.ClassSectionId ?? record.ClassSectionId;
                    if (targetClassId.HasValue && targetClassId.Value != Guid.Empty)
                    {
                        await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, targetClassId.Value);
                        return;
                    }

                    Guid? studentIdForClass = linkedSubmission?.StudentId ?? record.StudentId;
                    if (studentIdForClass.HasValue && studentIdForClass.Value != Guid.Empty)
                    {
                        await ClassSectionHelper.ValidateStudentOwnershipAsync(_unitOfWork, currentUser, studentIdForClass.Value);
                        return;
                    }
                }

                if (roles.Contains("student") || roles.Contains("parent"))
                {
                    Guid? targetStudentId = linkedSubmission?.StudentId ?? record.StudentId;
                    if (targetStudentId.HasValue && targetStudentId.Value != Guid.Empty)
                    {
                        await ClassSectionHelper.ValidateStudentOwnershipAsync(_unitOfWork, currentUser, targetStudentId.Value);
                        return;
                    }
                }

                throw new UnauthorizedAccessException("Access denied: You are not authorized to download this assignment submission.");
            }

            // 5. Check if referenced by a LessonNote or SchemeOfWorkEntry (instructional material)
            var linkedLessonNote = await _unitOfWork.GetRepository<LessonNote>().GetQueryable()
                .Where(l => l.FileUrl != null && l.FileUrl.Contains(fileIdToken))
                .Select(l => new { l.Id, l.ClassSectionId, l.Status })
                .FirstOrDefaultAsync();

            var linkedScheme = linkedLessonNote == null
                ? await _unitOfWork.GetRepository<SchemeOfWorkEntry>().GetQueryable()
                    .Where(s => s.MaterialsUrl != null && s.MaterialsUrl.Contains(fileIdToken))
                    .Select(s => new { s.Id, s.ClassSectionId })
                    .FirstOrDefaultAsync()
                : null;

            if (linkedLessonNote != null || linkedScheme != null || category == "lesson-notes" || category == "scheme")
            {
                if (roles.Contains("academic_head") || roles.Contains("teacher"))
                {
                    Guid? targetClassId = linkedLessonNote?.ClassSectionId ?? linkedScheme?.ClassSectionId ?? record.ClassSectionId;
                    if (targetClassId.HasValue && targetClassId.Value != Guid.Empty)
                    {
                        await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, targetClassId.Value);
                    }
                    return;
                }

                if (roles.Contains("student") || roles.Contains("parent"))
                {
                    if (linkedLessonNote != null && linkedLessonNote.Status != LessonNoteStatus.Approved)
                    {
                        throw new UnauthorizedAccessException("Access denied: This lesson note has not yet been approved for student publication.");
                    }

                    Guid? targetClassId = linkedLessonNote?.ClassSectionId ?? linkedScheme?.ClassSectionId ?? record.ClassSectionId;
                    if (targetClassId.HasValue && targetClassId.Value != Guid.Empty)
                    {
                        await ClassSectionHelper.ValidateClassAccessForPortalUserAsync(_unitOfWork, currentUser, targetClassId.Value);
                        return;
                    }
                }

                throw new UnauthorizedAccessException("Access denied: You are not authorized to download this instructional material.");
            }

            // 6. Fallback for any other category: check explicit student or class ownership if stamped on record
            if (record.StudentId.HasValue && record.StudentId.Value != Guid.Empty)
            {
                await ClassSectionHelper.ValidateStudentOwnershipAsync(_unitOfWork, currentUser, record.StudentId.Value);
                return;
            }

            if (record.ClassSectionId.HasValue && record.ClassSectionId.Value != Guid.Empty)
            {
                await ClassSectionHelper.ValidateClassAccessForPortalUserAsync(_unitOfWork, currentUser, record.ClassSectionId.Value);
                return;
            }

            throw new UnauthorizedAccessException("Access denied: You do not have permission to download this file.");
        }
    }
}
