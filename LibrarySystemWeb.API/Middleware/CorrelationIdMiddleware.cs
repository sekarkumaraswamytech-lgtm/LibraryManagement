using LibrarySystem.Application.Middleware;

namespace LibrarySystemWeb.API.Middleware
{
    /// <summary>
    /// Ensures every HTTP request has an x-correlation-id header.
    /// Populates CorrelationIdContext so StructuredLogger includes it.
    /// Adds the same header to the response for client tracing.
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        private const string HeaderName = "x-correlation-id";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var incoming = context.Request.Headers.TryGetValue(HeaderName, out var values)
                ? values.FirstOrDefault()
                : null;

            var correlationId = string.IsNullOrWhiteSpace(incoming) ? Guid.NewGuid().ToString("N") : incoming;

            // Store in AsyncLocal for structured logging
            CorrelationIdContext.Set(correlationId);

            // Ensure request & response both carry it
            context.Request.Headers[HeaderName] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            try
            {
                await _next(context);
            }
            finally
            {
                CorrelationIdContext.Clear();
            }
        }
    }
}