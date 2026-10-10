using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
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
}