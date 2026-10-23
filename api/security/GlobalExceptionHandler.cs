using Microsoft.AspNetCore.Diagnostics;

namespace ClothesShopping.Api.Security
{
    /// <summary>
    /// Catches all unhandled exceptions, logs the details, and returns a generic
    /// 500 response so that internal error messages are never exposed to clients.
    /// </summary>
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "An unhandled exception occurred.");

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/json";

            await httpContext.Response.WriteAsJsonAsync(
                new { message = "An unexpected error occurred." },
                cancellationToken);

            return true;
        }
    }
}
