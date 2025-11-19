using Grpc.Core;
using System.Text.Json;
using LibrarySystem.Application.Middleware;

namespace LibrarySystemWeb.API.Middleware
{
    public sealed class HttpExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        public HttpExceptionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (RpcException rpcEx)
            {
                // Map gRPC status codes coming from downstream gRPC clients
                ctx.Response.ContentType = "application/json";
                ctx.Response.StatusCode = rpcEx.StatusCode switch
                {
                    StatusCode.InvalidArgument => 400,
                    StatusCode.NotFound => 404,
                    StatusCode.PermissionDenied => 403,
                    StatusCode.Unauthenticated => 401,
                    StatusCode.Internal => 500,
                    _ => 500
                };

                var errorCode = rpcEx.Trailers.GetValue("error-code") ?? rpcEx.StatusCode.ToString();
                var payload = new
                {
                    error = errorCode,
                    message = rpcEx.Status.Detail,
                    grpcStatus = rpcEx.StatusCode.ToString(),
                    correlationId = ctx.Request.Headers["x-correlation-id"].FirstOrDefault()
                };
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
            }
            catch (CustomException ex)
            {
                // In case API directly calls services (future scenario)
                ctx.Response.ContentType = "application/json";
                ctx.Response.StatusCode = ex.Status;
                var payload = new
                {
                    error = ex.ErrorCode,
                    message = ex.Message,
                    status = ex.Status,
                    correlationId = ctx.Request.Headers["x-correlation-id"].FirstOrDefault()
                };
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
            }
            catch (Exception ex)
            {
                ctx.Response.ContentType = "application/json";
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "internal_error",
                    message = ex.Message,
                    correlationId = ctx.Request.Headers["x-correlation-id"].FirstOrDefault()
                }));
            }
        }
    }
}