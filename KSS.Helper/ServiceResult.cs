namespace KSS.Helper
{
    /// <summary>
    /// Result object for service operations that may fail due to business rules.
    /// Use instead of throwing BusinessRuleException — returns success/failure with message.
    /// The controller checks the result and returns the appropriate HTTP status code.
    /// </summary>
    public class ServiceResult
    {
        public bool Success { get; init; }
        public string? Message { get; init; }

        public static ServiceResult Ok() => new() { Success = true };
        public static ServiceResult Fail(string message) => new() { Success = false, Message = message };
    }
}
