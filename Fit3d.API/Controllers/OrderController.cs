using System;
using System.Threading.Tasks;
using Fit3d.BLL.DTOs;
using Fit3d.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fit3d.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrderController(IOrderService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaging([FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] Guid? userId = null, [FromQuery] int? status = null)
        {
            var result = await _service.GetPagingAsync(page, size, userId, status);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDTO createDto)
        {
            try
            {
                var result = await _service.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrderDTO updateDto)
        {
            var result = await _service.UpdateAsync(id, updateDto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }
}
