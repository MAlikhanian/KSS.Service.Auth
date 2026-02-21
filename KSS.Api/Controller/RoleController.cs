using KSS.Dto;
using KSS.Entity;
using KSS.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KSS.Api.Controller
{
    [ApiController]
    [Route("Api/[controller]/[action]")]
    [Authorize]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpPost]
        public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleRequestDto request)
        {
            var role = await _roleService.CreateRoleAsync(request);
            return Ok(role);
        }

        [HttpGet]
        public async Task<ActionResult<List<RoleDto>>> GetAll()
        {
            var roles = await _roleService.GetAllRolesWithPermissionsAsync();
            return Ok(roles);
        }

        [HttpPost]
        public async Task<ActionResult> AssignRolesToUser([FromBody] AssignRoleRequestDto request)
        {
            await _roleService.AssignRolesToUserAsync(request);
            return Ok(new { message = "Roles assigned successfully" });
        }

        [HttpPost]
        public async Task<ActionResult> AssignPermissions([FromBody] AssignPermissionsRequestDto request)
        {
            await _roleService.AssignPermissionsToRoleAsync(request.RoleId, request.PermissionIds);
            return Ok(new { message = "Permissions assigned successfully" });
        }
    }

    public class AssignPermissionsRequestDto
    {
        public Guid RoleId { get; set; }
        public List<Guid> PermissionIds { get; set; } = new();
    }
}
