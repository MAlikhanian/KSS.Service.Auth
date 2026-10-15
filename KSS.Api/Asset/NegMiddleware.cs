using KSS.Helper;

namespace KSS.Api.Asset
{
    public class NegMiddleware
    {
        private readonly RequestDelegate _requestDelegate;
        public NegMiddleware(RequestDelegate requestDelegate) => _requestDelegate = requestDelegate;
        public async Task Invoke(HttpContext httpContext, GlobalProperty globalProperty)
        {
            // TODO: resolve current user from Person microservice when Negotiate auth is used
            await _requestDelegate(httpContext);
        }
    }
}
