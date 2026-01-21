using BLL.Interfaces;
using DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TaskController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignTasks([FromBody] AssignTaskRequest request)
        {
            await _taskService.AssignTasksToAnnotatorAsync(request);
            return Ok(new { Message = "Tasks assigned successfully" });
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetMyStats()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Bạn cần thêm hàm này vào Interface ITaskService trước
            var stats = await _taskService.GetAnnotatorStatsAsync(userId);
            return Ok(stats);
        }

        [HttpGet("my-tasks")]
        public async Task<IActionResult> GetMyTasks([FromQuery] int projectId = 0, [FromQuery] string? status = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var tasks = await _taskService.GetMyTasksAsync(projectId, userId, status);
            return Ok(tasks);
        }

        [HttpGet("detail/{assignmentId}")]
        public async Task<IActionResult> GetTaskDetail(int assignmentId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var task = await _taskService.GetTaskDetailAsync(assignmentId, userId);
            if (task == null) return NotFound();
            return Ok(task);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitTask([FromBody] SubmitAnnotationRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _taskService.SubmitTaskAsync(userId, request);
            return Ok(new { Message = "Task submitted successfully" });
        }
    }
}