using Microsoft.Extensions.Primitives;
using System.IdentityModel.Tokens.Jwt;

namespace KSS.Api.Asset
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _requestDelegate;
        public JwtMiddleware(RequestDelegate requestDelegate) => _requestDelegate = requestDelegate;
        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
            {
                var token = authHeader.FirstOrDefault()?.Split(" ").Last();
                if (!string.IsNullOrEmpty(token))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
                    if (jwtToken != null)
                    {
                        var loginName = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "userName")?.Value;
                        if (!string.IsNullOrEmpty(loginName))
                        {
                            // TODO: resolve current user from Person microservice when needed
                        }
                    }
                }
            }

            await _requestDelegate(httpContext);
        }
    }
}
