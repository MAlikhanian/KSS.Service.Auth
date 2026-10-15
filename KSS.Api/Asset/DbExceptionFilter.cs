using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using KSS.Helper;
using KSS.Service.IService;

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
            // Account administration refused for this caller and target.
            if (context.Exception is AdminActionRefusedException refused)
            {
                context.Result = new ObjectResult(new { code = refused.Code, message = refused.Message })
                {
                    StatusCode = 403
                };
                context.ExceptionHandled = true;
                return;
            }

            // The change would leave a maximal permission set without an active holder.
            if (context.Exception is LastMaximalHolderException lastHolder)
            {
                context.Result = new ObjectResult(new
                {
                    code = LastMaximalHolderException.ErrorCode,
                    message = lastHolder.Message,
                    permissions = lastHolder.Permissions
                })
                {
                    StatusCode = 409
                };
                context.ExceptionHandled = true;
                return;
            }

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

            // Handle DbUpdateConcurrencyException. Checked before DbUpdateException,
            // which it derives from, so it is not absorbed by the general case.
            if (context.Exception is DbUpdateConcurrencyException)
            {
                context.Result = new ObjectResult(new { message = "CONCURRENCY_ERROR" })
                {
                    StatusCode = 409
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
        }
    }
}
