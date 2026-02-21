using KSS.Dto;
using KSS.Entity;
using KSS.Service.IService;
using KSS.API.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KSS.Api.Controller
{
    [ApiController]
    [Route("Api/[controller]/[action]")]
    [Authorize]
    public class PermissionController : BaseController<Permission, PermissionDto, PermissionDto, PermissionDto>
    {
        public PermissionController(IBaseService<Permission, PermissionDto, PermissionDto, PermissionDto> service) : base(service)
        {
        }
    }
}
