using System.Reflection;
using System.Text.Json;
using AutoMapper;
using KSS.Api.Controller;
using KSS.Api.MappingProfile;
using KSS.Dto;
using KSS.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KSS.Api.Tests
{
    /// <summary>
    /// UserDto is the body of Register, GetCurrentUser and ByPersonId (the last one about OTHER users), and is nested
    /// in the Login response. It must never carry a credential: not the password hash, not the refresh token, not the
    /// reset token. The two expiry timestamps stay (the Person zone's security card reads RefreshTokenExpires).
    /// </summary>
    public class UserDtoCredentialFieldTests
    {
        private const string HashValue = "HASH-VALUE-5c1e";
        private const string RefreshValue = "REFRESH-VALUE-9a2b";
        private const string ResetValue = "RESET-VALUE-77d0";

        [Theory]
        [InlineData("PasswordHash")]
        [InlineData("RefreshToken")]
        [InlineData("PasswordResetToken")]
        public void UserDto_has_no_credential_property(string property)
        {
            Assert.Null(typeof(UserDto).GetProperty(property));
        }

        [Theory]
        [InlineData("RefreshTokenExpires")]
        [InlineData("PasswordResetExpires")]
        public void UserDto_keeps_the_expiry_timestamps(string property)
        {
            Assert.NotNull(typeof(UserDto).GetProperty(property));
        }

        [Theory]
        [InlineData(nameof(UserController.ByPersonId))]
        [InlineData(nameof(UserController.Register))]
        [InlineData(nameof(UserController.GetCurrentUser))]
        public void The_endpoint_body_is_a_UserDto_and_its_json_carries_no_credential(string action)
        {
            var method = typeof(UserController).GetMethod(action, BindingFlags.Public | BindingFlags.Instance)!;
            Assert.Equal(typeof(Task<ActionResult<UserDto>>), method.ReturnType);

            var json = JsonSerializer.Serialize(MapThroughTheRealProfile(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
            AssertNoCredential(json);
        }

        [Fact]
        public void The_login_response_nests_a_UserDto_whose_json_carries_no_credential()
        {
            var response = new AuthResponseDto { User = MapThroughTheRealProfile() };
            var json = JsonSerializer.Serialize(response.User, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            AssertNoCredential(json);
        }

        private static UserDto MapThroughTheRealProfile()
        {
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new BaseMappingProfile()), NullLoggerFactory.Instance)
                .CreateMapper();
            var user = new User
            {
                Id = Guid.CreateVersion7(),
                Username = "0000000000",
                Email = "dto.test@example.test",
                PasswordHash = HashValue,
                RefreshToken = RefreshValue,
                RefreshTokenExpires = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                PasswordResetToken = ResetValue,
                PasswordResetExpires = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
            };
            return mapper.Map<UserDto>(user);
        }

        private static void AssertNoCredential(string json)
        {
            Assert.DoesNotContain(HashValue, json);
            Assert.DoesNotContain(RefreshValue, json);
            Assert.DoesNotContain(ResetValue, json);
            Assert.DoesNotContain("\"passwordHash\"", json);
            Assert.DoesNotContain("\"refreshToken\"", json);
            Assert.DoesNotContain("\"passwordResetToken\"", json);
            Assert.Contains("\"refreshTokenExpires\"", json);
        }
    }
}
