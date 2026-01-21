using BLL.Exceptions;
using System.Net;
using System.Text.Json;

namespace API.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

            context.Response.ContentType = "application/json";

            var response = new { message = exception.Message };
            int statusCode;

            switch (exception)
            {
                case NotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    break;
                case ValidationException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    break;
                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    response = new { message = "An internal server error occurred." };
                    break;
            }

            // In development, we might want to see the real error even for 500
            // But for this task, consistency is key.
            // Let's stick to the message from the exception if it's not 500, or generic if 500.
            // Actually, for "logic errors that FE could have hard time", getting a 500 with "Internal Server Error" is better than a random stack trace in HTML.
            // But if I want to be helpful to myself during debug, I might want the real message.
            // Let's keep the real message for now for debugging purposes if it's not sensitive,
            // but usually 500 implies unexpected.

            // Re-evaluating default case:
            if (exception is not NotFoundException &&
                exception is not ValidationException &&
                exception is not UnauthorizedAccessException)
            {
                 // Check if it's a generic Exception with a message meant for user
                 // For now, let's just return the exception message.
                 // The user's request is "looking For error or logic error".
                 // If I hide the error, it's harder to debug.
                 // But consistent JSON is the goal.
                 response = new { message = exception.Message };
            }

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
