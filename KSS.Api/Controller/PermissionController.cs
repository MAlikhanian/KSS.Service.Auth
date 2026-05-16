using KSS.Dto;
using KSS.Entity;
using KSS.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KSS.Api.Controller
{
    /// <summary>
    /// Read-only Permission API. Permission data is critical — write paths are
    /// intentionally NOT exposed. The catalog is maintained directly in the
    /// database by a DBA via versioned migration scripts.
    /// </summary>
    [ApiController]
    [Route("Api/[controller]/[action]")]
    [Authorize]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionService _service;

        public PermissionController(IPermissionService service)
        {
            _service = service;
        }

        /// <summary>GET /Api/Permission/ToListAll — list all permissions (read-only).</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Permission>>> ToListAll()
        {
            var data = await _service.ToListAsync();
            return Ok(data);
        }
    }
}
