namespace HabitTracker.Api.Middleware
{
    public class ExceptionalMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionalMiddleware> _logger;

        public ExceptionalMiddleware(RequestDelegate next, ILogger<ExceptionalMiddleware> logger)
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
                _logger.LogError(ex, "An unhandled exception on {Method} {Path}.",context.Request.Method, context.Request.Path);
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("An unexpected error occurred. Please try again later.");
            }
        }
    }
}
