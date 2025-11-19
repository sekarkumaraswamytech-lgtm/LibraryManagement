namespace LibrarySystem.Application.Middleware
{
    // Custom exception base
    public class CustomException : Exception
    {
        public string ErrorCode { get; }
        public int Status { get; }

        public CustomException(string message, string errorCode, int status = 400, Exception? inner = null)
            : base(message, inner)
        {
                ErrorCode = errorCode;
                Status = status;
        }
    }

    public sealed class NotFoundException : CustomException
    {
        public NotFoundException(string message, string errorCode = "not_found")
            : base(message, errorCode, 404) { }
    }

    public sealed class ValidationException : CustomException
    {
        public ValidationException(string message, string errorCode = "validation_error")
            : base(message, errorCode, 400) { }
    }

    // New: Data access layer failures
    public sealed class DataAccessException : CustomException
    {
        public DataAccessException(string message, string errorCode = "data_access_error", Exception? inner = null, int status = 500)
            : base(message, errorCode, status, inner) { }
    }
}
