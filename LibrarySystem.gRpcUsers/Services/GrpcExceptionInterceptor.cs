using Grpc.Core;
using Grpc.Core.Interceptors;
using LibrarySystem.Application.Middleware;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.gRpcUsers.Services
{
    /// <summary>
    /// Captures correlation id, maps CustomException to RpcException, and logs with correlation context.
    /// </summary>
    public sealed class GrpcExceptionInterceptor : Interceptor
    {
        private readonly ILogger<GrpcExceptionInterceptor> _logger;
        private const string CorrelationHeader = "x-correlation-id";

        public GrpcExceptionInterceptor(ILogger<GrpcExceptionInterceptor> logger) => _logger = logger;

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var correlationId = context.RequestHeaders.FirstOrDefault(h => h.Key == CorrelationHeader)?.Value
                                ?? Guid.NewGuid().ToString("N");
            CorrelationIdContext.Set(correlationId);

            try
            {
                return await continuation(request, context);
            }
            catch (CustomException ex)
            {
                var rpc = Map(ex, correlationId);
                _logger.LogWarning("Mapped custom exception {Code}: {Message} (corr={CorrelationId})",
                    ex.ErrorCode, ex.Message, correlationId);
                throw rpc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception (corr={CorrelationId})", correlationId);
                throw new RpcException(
                    new Status(StatusCode.Internal, "Internal server error"),
                    BuildMetadata(correlationId, "internal_error", "500"));
            }
            finally
            {
                CorrelationIdContext.Clear();
            }
        }

        private static RpcException Map(CustomException ex, string correlationId)
        {
            var code = ex switch
            {
                ValidationException => StatusCode.InvalidArgument,
                NotFoundException => StatusCode.NotFound,
                DataAccessException => StatusCode.Internal,
                _ => StatusCode.Unknown
            };
            return new RpcException(new Status(code, ex.Message),
                BuildMetadata(correlationId, ex.ErrorCode, ex.Status.ToString()));
        }

        private static Metadata BuildMetadata(string correlationId, string errorCode, string status) =>
            new()
            {
                { "error-code", errorCode },
                { "status", status },
                { CorrelationHeader, correlationId }
            };
    }
}