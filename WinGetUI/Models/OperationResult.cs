namespace WinGetUI.Models
{
    /// <summary>
    /// Represents the result of a package operation
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public int? ExitCode { get; set; }
        public string Output { get; set; }

        public OperationResult()
        {
            Success = false;
            Message = string.Empty;
            ErrorMessage = string.Empty;
            Output = string.Empty;
        }

        public static OperationResult CreateSuccess(string message = "Operation completed successfully")
        {
            return new OperationResult
            {
                Success = true,
                Message = message
            };
        }

        public static OperationResult CreateError(string errorMessage, int? exitCode = null)
        {
            return new OperationResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ExitCode = exitCode
            };
        }
    }
}
