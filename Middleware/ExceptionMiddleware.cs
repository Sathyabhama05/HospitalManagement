using System.Net;
using System.Text.Json;

namespace HospitalManagement.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // pass request to next middleware
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var statusCode = ex switch
            {
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,   // 401
                KeyNotFoundException        => HttpStatusCode.NotFound,        // 404
                ArgumentException           => HttpStatusCode.BadRequest,      // 400
                _                           => HttpStatusCode.InternalServerError // 500
            };

            var response = new
            {
                status  = (int)statusCode,
                message = ex.Message
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode  = (int)statusCode;

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}