using BLL.Interfaces;
using DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing user profiles and administration.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Gets the profile of the current logged-in user.
        /// </summary>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound(new { Message = "User not found" });

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                PaymentInfo = user.PaymentInfo != null ? new
                {
                    user.PaymentInfo.BankName,
                    user.PaymentInfo.BankAccountNumber,
                    user.PaymentInfo.TaxCode
                } : null
            });
        }

        // --- 👇 ĐOẠN CODE CẦN THÊM VÀO 👇 ---

        /// <summary>
        /// Updates the profile (Name, Password) of the current user.
        /// </summary>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                // Bảo mật: Không cho phép user tự đổi Role của mình
                request.Role = null;

                // Tái sử dụng hàm UpdateUserAsync của Service
                await _userService.UpdateUserAsync(userId, request);
                return Ok(new { Message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        // ----------------------------------------

        /// <summary>
        /// Updates the payment information of the current user.
        /// </summary>
        [HttpPut("payment")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> UpdateMyPaymentInfo([FromBody] UpdatePaymentRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                await _userService.UpdatePaymentInfoAsync(userId, request.BankName, request.BankAccountNumber, request.TaxCode);
                return Ok(new { Message = "Payment info updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ... Các API Admin khác giữ nguyên ...

        [HttpGet]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequest request)
        {
            try
            {
                var user = await _userService.RegisterAsync(request.FullName, request.Email, request.Password, request.Role);
                return Ok(new { Message = "User created successfully", UserId = user.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                await _userService.UpdateUserAsync(id, request);
                return Ok(new { Message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return Ok(new { Message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}