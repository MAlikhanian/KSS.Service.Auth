using System.Diagnostics;

namespace KSS.Helper
{
    /// <summary>
    /// Represents a business rule violation — NOT a system error.
    /// The middleware returns HTTP 400 with the message so the user sees a clean notification.
    /// Use this instead of InvalidOperationException for business rules.
    /// </summary>
    public class BusinessRuleException : Exception
    {
        [DebuggerHidden]
        public BusinessRuleException(string message) : base(message) { }
    }
}
