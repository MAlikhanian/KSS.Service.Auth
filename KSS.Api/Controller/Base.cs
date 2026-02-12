using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KSS.Helper;
using KSS.Helper.Model;
using KSS.Service.IService;

namespace KSS.API.Controller
{
    [ApiController]
    [Route("Api/[controller]/[action]")]
    [Authorize]
    public class BaseController<T, TViewDto, TAddDto, TUpdateDto> : ControllerBase
        where T : class
        where TViewDto : class
        where TAddDto : class
        where TUpdateDto : class
    {
        private readonly IBaseService<T, TViewDto, TAddDto, TUpdateDto> _service;
        public BaseController(IBaseService<T, TViewDto, TAddDto, TUpdateDto> service) => _service = service;

        [HttpPost]
        public async Task<ActionResult> FindAsync([FromBody] Filter id) => Ok(await _service.FindAsync(id));

        [HttpPost]
        public async Task<ActionResult> SingleAsync([FromBody] T filter) => Ok(await _service.SingleAsync(filter));

        [HttpGet]
        public async Task<ActionResult> ToListAllAsync() => Ok(await _service.ToListAsync());

        [HttpPost]
        public async Task<ActionResult> ToListAsync([FromBody] T filter) => Ok(await _service.ToListAsync(filter));

        [HttpPost]
        public async Task<ActionResult> ToListByFilterAsync([FromBody] Filter filter) => Ok(await _service.ToListAsync(filter));

        [HttpPost]
        public async Task<ActionResult> ToListDtoAsync([FromBody] T filter) => Ok(_service.Dto(await _service.ToListAsync(filter)));

        [HttpPost]
        public async Task<ActionResult> AddAsync([FromBody] T item)
        {
            await _service.AddAsync(item);

            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult> AddDtoAsync([FromBody] TAddDto item)
        {
            await _service.AddDtoAsync(item);

            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult> AddRangeAsync([FromBody] IEnumerable<T> items)
        {
            await _service.AddRangeAsync(items);

            return Ok(items);
        }

        [HttpPut]
        public IActionResult Update([FromBody] T item)
        {
            _service.Update(item);

            return NoContent();
        }

        [HttpPut]
        public IActionResult UpdateDto([FromBody] TUpdateDto item)
        {
            _service.UpdateDto(item);

            return NoContent();
        }

        [HttpPut]
        public IActionResult UpdateRange([FromBody] IEnumerable<T> items)
        {
            _service.UpdateRange(items);

            return NoContent();
        }

        [HttpDelete()]
        public IActionResult Remove([FromBody] T item)
        {
            _service.Remove(item);

            return NoContent();
        }

        [HttpDelete]
        public IActionResult RemoveRange([FromBody] IEnumerable<T> items)
        {
            _service.RemoveRange(items);

            return NoContent();
        }
    }
}