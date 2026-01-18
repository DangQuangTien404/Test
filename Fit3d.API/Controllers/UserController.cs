using System;
using System.Threading.Tasks;
using Fit3d.BLL.DTOs;
using Fit3d.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fit3d.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;

        public UserController(IUserService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaging([FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string? search = null)
        {
            var result = await _service.GetPagingAsync(page, size, search);
            return Ok(result);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
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
        public async Task<IActionResult> Create([FromBody] CreateUserDTO createDto)
        {
            var result = await _service.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDTO updateDto)
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

        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDTO changePasswordDto)
        {
            var result = await _service.ChangePasswordAsync(id, changePasswordDto);
            if (!result) return BadRequest("Password change failed. Check old password or user ID.");
            return NoContent();
        }
    }
}
