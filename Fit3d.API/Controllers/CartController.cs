using System;
using System.Threading.Tasks;
using Fit3d.BLL.DTOs;
using Fit3d.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fit3d.API.Controllers
{
    [ApiController]
    [Route("api/cart")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _service;

        public CartController(ICartService service)
        {
            _service = service;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetCart(Guid userId)
        {
            var result = await _service.GetCartByUserIdAsync(userId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] CreateCartItemDTO createDto)
        {
            try
            {
                var result = await _service.AddToCartAsync(createDto);
                return CreatedAtAction(nameof(GetCart), new { userId = createDto.UserId }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuantity(Guid id, [FromBody] UpdateCartItemDTO updateDto)
        {
            var result = await _service.UpdateQuantityAsync(id, updateDto.Quantity);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveItem(Guid id)
        {
            var result = await _service.RemoveFromCartAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpDelete("user/{userId}")]
        public async Task<IActionResult> ClearCart(Guid userId)
        {
            await _service.ClearCartAsync(userId);
            return NoContent();
        }
    }
}
