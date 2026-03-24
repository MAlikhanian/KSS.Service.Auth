namespace KSS.Helper
{
    /// <summary>
    /// Maps SQL error numbers and messages to BusinessRuleException error codes.
    /// No external dependencies — works with raw exception message strings.
    /// </summary>
    public static class SqlExceptionHandler
    {
        /// <summary>
        /// Translates a database exception message to a user-friendly error code.
        /// Call this in catch blocks: throw new BusinessRuleException(SqlExceptionHandler.GetErrorCode(ex));
        /// </summary>
        public static string GetErrorCode(Exception ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;

            // Unique key violation (SQL Error 2601/2627)
            if (message.Contains("UNIQUE") || message.Contains("duplicate key") || message.Contains("Cannot insert duplicate"))
            {
                if (message.Contains("NationalId")) return "DUPLICATE_NATIONAL_ID";
                if (message.Contains("Email")) return "DUPLICATE_EMAIL";
                if (message.Contains("Phone")) return "DUPLICATE_PHONE";
                return "DUPLICATE_RECORD";
            }

            // Foreign key violation (SQL Error 547)
            if (message.Contains("FOREIGN KEY") || message.Contains("conflicted with"))
            {
                return "FOREIGN_KEY_VIOLATION";
            }

            // Cannot insert NULL (SQL Error 515)
            if (message.Contains("Cannot insert the value NULL"))
            {
                return "VALIDATION_ERROR";
            }

            return "SERVER_ERROR";
        }
    }
}
