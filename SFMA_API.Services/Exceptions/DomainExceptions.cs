using System;

namespace SFMA_API.Services.Exceptions
{
    public class DuplicateRecordException : Exception
    {
        public string CollisionField { get; }
        public object? ExistingScholar { get; }

        public DuplicateRecordException(string collisionField, object? existingScholar = null, string message = "A duplicate record was found.")
            : base(message)
        {
            CollisionField = collisionField;
            ExistingScholar = existingScholar;
        }
    }

    public class DeadlineElapsedException : Exception
    {
        public string DeadlineCode { get; }

        public DeadlineElapsedException(string deadlineCode, string message = "The submission deadline for this assessment has elapsed.")
            : base(message)
        {
            DeadlineCode = deadlineCode;
        }
    }

    public class RecordLockedException : Exception
    {
        public RecordLockedException(string message = "This attendance register is locked. Supervisor authorization is required to unlock it for roll correction.")
            : base(message)
        {
        }
    }

    public class InvalidSubmissionFormatException : Exception
    {
        public string RequiredFormat { get; }

        public InvalidSubmissionFormatException(string requiredFormat, string message = "Uploaded file format does not match the required submission mode.")
            : base(message)
        {
            RequiredFormat = requiredFormat;
        }
    }
}
