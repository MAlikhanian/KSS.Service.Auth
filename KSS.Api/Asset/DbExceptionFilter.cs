using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using KSS.Helper;

namespace KSS.Api.Asset
{
    /// <summary>
    /// Global exception filter that catches DbUpdateException from ALL controllers
    /// and returns clean error codes (HTTP 400) instead of raw SQL errors (HTTP 500).
    /// Frontend uses these codes to look up translated messages via i18n.
    /// </summary>
    public class DbExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            // Handle BusinessRuleException — already clean
            if (context.Exception is BusinessRuleException brEx)
            {
                context.Result = new ObjectResult(new { message = brEx.Message })
                {
                    StatusCode = 400
                };
                context.ExceptionHandled = true;
                return;
            }

            // Handle DbUpdateException — translate SQL errors to codes
            if (context.Exception is DbUpdateException dbEx)
            {
                var code = SqlExceptionHandler.GetErrorCode(dbEx);
                context.Result = new ObjectResult(new { message = code })
                {
                    StatusCode = 400
                };
                context.ExceptionHandled = true;
                return;
            }

            // Handle DbUpdateConcurrencyException
            if (context.Exception is DbUpdateConcurrencyException)
            {
                context.Result = new ObjectResult(new { message = "CONCURRENCY_ERROR" })
                {
                    StatusCode = 409
                };
                context.ExceptionHandled = true;
                return;
            }
        }
    }
}
