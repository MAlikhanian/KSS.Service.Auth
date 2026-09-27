using KSS.Api.Asset;
using KSS.Helper;
using KSS.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KSS.Api.Tests
{
    public class ExceptionFilterMappingTests
    {
        private static ObjectResult Map(Exception exception)
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            var context = new ExceptionContext(actionContext, new List<IFilterMetadata>()) { Exception = exception };

            new DbExceptionFilter().OnException(context);

            Assert.True(context.ExceptionHandled);
            return Assert.IsType<ObjectResult>(context.Result);
        }

        private static object? Field(ObjectResult result, string name)
            => result.Value!.GetType().GetProperty(name)!.GetValue(result.Value);

        [Fact]
        public void Refused_administration_maps_to_403_with_its_code()
        {
            var result = Map(new AdminActionRefusedException("TARGET_NOT_COVERED", "m"));
            Assert.Equal(403, result.StatusCode);
            Assert.Equal("TARGET_NOT_COVERED", Field(result, "code"));
        }

        [Fact]
        public void Last_maximal_holder_maps_to_409_with_the_set()
        {
            var result = Map(new LastMaximalHolderException(new[] { "a", "b" }));
            Assert.Equal(409, result.StatusCode);
            Assert.Equal(LastMaximalHolderException.ErrorCode, Field(result, "code"));
            Assert.Equal(new[] { "a", "b" }, (IEnumerable<string>)Field(result, "permissions")!);
        }

        [Fact]
        public void Concurrency_failure_maps_to_409_not_to_the_general_database_case()
        {
            var result = Map(new DbUpdateConcurrencyException("m"));
            Assert.Equal(409, result.StatusCode);
            Assert.Equal("CONCURRENCY_ERROR", Field(result, "message"));
        }

        [Fact]
        public void General_database_failure_still_maps_to_400()
        {
            var result = Map(new DbUpdateException("m"));
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public void Business_rule_still_maps_to_400()
        {
            var result = Map(new BusinessRuleException("UNKNOWN_ROLE"));
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("UNKNOWN_ROLE", Field(result, "message"));
        }
    }
}
