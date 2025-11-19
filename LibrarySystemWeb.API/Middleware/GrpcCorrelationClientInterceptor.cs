using Grpc.Core;
using Grpc.Core.Interceptors;
using LibrarySystem.Application.Middleware;

namespace LibrarySystemWeb.API.Middleware
{
    /// <summary>
    /// Adds x-correlation-id metadata to outgoing gRPC client calls based on current HTTP request context.
    /// </summary>
    public sealed class GrpcCorrelationClientInterceptor : Interceptor
    {
        private const string HeaderName = "x-correlation-id";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GrpcCorrelationClientInterceptor(IHttpContextAccessor accessor) => _httpContextAccessor = accessor;

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var headers = context.Options.Headers ?? new Metadata();

            var correlationId =
                _httpContextAccessor.HttpContext?.Request.Headers[HeaderName].FirstOrDefault() ??
                CorrelationIdContext.Current ??
                Guid.NewGuid().ToString("N");

            if (!headers.Any(h => h.Key.Equals(HeaderName, StringComparison.OrdinalIgnoreCase)))
            {
                headers.Add(HeaderName, correlationId);
            }

            var newOptions = context.Options.WithHeaders(headers);
            var newContext = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, newOptions);
            return base.AsyncUnaryCall(request, newContext, continuation);
        }
    }
}