using KSS.Dto;
using KSS.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KSS.Api.Controller
{
    /// <summary>
    /// Read-only Role catalog API. Role + RolePermission rows are managed via
    /// database migrations only — the controller only exposes a list endpoint:
    ///   * GET /Api/Role/GetAll — list catalog with permissions
    /// User→Role assignment lives in UserController.AssignRoles.
    /// </summary>
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

        [HttpGet]
        public async Task<ActionResult<List<RoleDto>>> GetAll()
        {
            var roles = await _roleService.GetAllRolesWithPermissionsAsync();
            return Ok(roles);
        }
    }
}
