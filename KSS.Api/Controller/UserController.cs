using System.Security.Claims;
using KSS.Dto;
using KSS.Helper;
using KSS.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KSS.Api.Controller
{
    [ApiController]
    [Route("Api/[controller]/[action]")]
    public class UserController : ControllerBase
    {
        private const string CaptchaHeaderName = "X-Captcha-Payload";

        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly ICaptchaClient _captchaClient;
        private readonly IConfiguration _configuration;

        public UserController(IUserService userService, IRoleService roleService, ICaptchaClient captchaClient, IConfiguration configuration)
        {
            _userService = userService;
            _roleService = roleService;
            _captchaClient = captchaClient;
            _configuration = configuration;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequestDto request)
        {
            if (!await VerifyCaptchaAsync())
                return BadRequest(new { message = "Captcha verification failed." });

            try
            {
                var user = await _userService.RegisterAsync(request);
                return Ok(user);
            }
            catch (BusinessRuleException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during registration", error = ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (!await VerifyCaptchaAsync())
                return BadRequest(new { message = "Captcha verification failed." });

            try
            {
                var jwtSecret = _configuration["JWT_SECRET"]
                    ?? _configuration["TokenSetting:JwtSecret"]
                    ?? _configuration["TokenSetting__JwtSecret"];

                if (string.IsNullOrEmpty(jwtSecret))
                {
                    return StatusCode(500, new { message = "JWT Secret is not configured" });
                }

                var authResponse = await _userService.LoginAsync(request, jwtSecret);
                return Ok(authResponse);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during login", error = ex.Message });
            }
        }

        private async Task<bool> VerifyCaptchaAsync()
        {
            if (!Request.Headers.TryGetValue(CaptchaHeaderName, out var values))
                return false;
            return await _captchaClient.VerifyAsync(values.ToString());
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var user = await _userService.GetByUsernameAsync(username);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        // ─── Security management (powers the /person/security page) ───

        /// <summary>GET /Api/User/ByPersonId/{personId} — returns the User row tied to a PersonId, or 404.</summary>
        [HttpGet("{personId}")]
        [Authorize]
        public async Task<ActionResult<UserDto>> ByPersonId(Guid personId)
        {
            var user = await _userService.GetByPersonIdAsync(personId);
            if (user == null) return NotFound(new { message = "No user account linked to this person" });
            return Ok(user);
        }

        /// <summary>
        /// POST /Api/User/MapPersonsToUsers — bulk version of ByPersonId. Body:
        /// { personIds: [...] }. Returns a map of PersonId → UserId (only inputs
        /// that have a User row appear in the response). Powers dashboard report
        /// endpoints in Company and Person services. Requires authentication;
        /// available to any logged-in user (no specific permission).
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<IDictionary<Guid, Guid>>> MapPersonsToUsers([FromBody] PersonsToUsersRequestDto dto)
        {
            var map = await _userService.MapPersonsToUsersAsync(dto?.PersonIds ?? new List<Guid>());
            return Ok(map);
        }

        /// <summary>PUT /Api/User/ChangePassword — self-service password change.</summary>
        [HttpPut]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var callerUserId = GetCallerUserId();
            if (callerUserId == null) return Unauthorized();
            await _userService.ChangePasswordAsync(callerUserId.Value, dto.CurrentPassword, dto.NewPassword);
            return NoContent();
        }

        /// <summary>PUT /Api/User/AdminResetPassword — admin override, no current password required.</summary>
        [HttpPut]
        [Authorize]
        public async Task<ActionResult> AdminResetPassword([FromBody] AdminResetPasswordDto dto)
        {
            await _userService.AdminResetPasswordAsync(dto.UserId, dto.NewPassword);
            return NoContent();
        }

        /// <summary>POST /Api/User/Lock/{userId} — admin lock for N minutes.</summary>
        [HttpPost("{userId}")]
        [Authorize]
        public async Task<ActionResult> Lock(Guid userId, [FromBody] LockUserDto dto)
        {
            await _userService.LockAsync(userId, dto.LockMinutes);
            return NoContent();
        }

        /// <summary>POST /Api/User/Unlock/{userId} — clear lock + reset failed attempts.</summary>
        [HttpPost("{userId}")]
        [Authorize]
        public async Task<ActionResult> Unlock(Guid userId)
        {
            await _userService.UnlockAsync(userId);
            return NoContent();
        }

        /// <summary>POST /Api/User/MarkEmailVerified/{userId} — admin mark email verified.</summary>
        [HttpPost("{userId}")]
        [Authorize]
        public async Task<ActionResult> MarkEmailVerified(Guid userId)
        {
            await _userService.MarkEmailVerifiedAsync(userId);
            return NoContent();
        }

        /// <summary>POST /Api/User/MarkPhoneVerified/{userId} — admin mark phone verified.</summary>
        [HttpPost("{userId}")]
        [Authorize]
        public async Task<ActionResult> MarkPhoneVerified(Guid userId)
        {
            await _userService.MarkPhoneVerifiedAsync(userId);
            return NoContent();
        }

        /// <summary>PUT /Api/User/SetActive/{userId} — admin toggle IsActive.</summary>
        [HttpPut("{userId}")]
        [Authorize]
        public async Task<ActionResult> SetActive(Guid userId, [FromBody] SetActiveDto dto)
        {
            await _userService.SetActiveAsync(userId, dto.IsActive);
            return NoContent();
        }

        /// <summary>POST /Api/User/RevokeSessions/{userId} — clear refresh token (forces logout).</summary>
        [HttpPost("{userId}")]
        [Authorize]
        public async Task<ActionResult> RevokeSessions(Guid userId)
        {
            await _userService.RevokeSessionsAsync(userId);
            return NoContent();
        }

        /// <summary>GET /Api/User/UserRoles/{userId} — list roles assigned to the user.</summary>
        [HttpGet("{userId}")]
        [Authorize]
        public async Task<ActionResult<List<string>>> UserRoles(Guid userId)
        {
            var roles = await _roleService.GetUserRoleNamesAsync(userId);
            return Ok(roles);
        }

        /// <summary>PUT /Api/User/AssignRoles — replace role assignments for a user.</summary>
        [HttpPut]
        [Authorize]
        public async Task<ActionResult> AssignRoles([FromBody] AssignRoleRequestDto request)
        {
            await _roleService.AssignRolesToUserAsync(request);
            return NoContent();
        }

        // Resolve the caller's userId from the JWT. Falls back to looking up by username
        // since the existing JWT claim set uses "name" for username, "personId" for PersonId,
        // but no explicit userId claim.
        private Guid? GetCallerUserId()
        {
            var raw = User.FindFirstValue("userId");
            if (Guid.TryParse(raw, out var id)) return id;

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;
            var user = _userService.SingleOrDefault(u => u.Username == username);
            return user?.Id;
        }
    }
}
