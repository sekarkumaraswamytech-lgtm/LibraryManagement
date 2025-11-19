using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace LibrarySystem.Application.Middleware
{
    public interface IStructuredLogger
    {
        void Info(string message, object? data = null, [CallerMemberName] string? member = null);
        void Warn(string message, object? data = null, [CallerMemberName] string? member = null);
        void Error(string message, Exception ex, object? data = null, [CallerMemberName] string? member = null);
    }

    public sealed class StructuredLogger<T> : IStructuredLogger
    {
        private readonly ILogger<T>? _logger;
        public StructuredLogger(ILogger<T>? logger = null) => _logger = logger;

        public void Info(string message, object? data = null, string? member = null) =>
            Write(LogLevel.Information, message, data, member);

        public void Warn(string message, object? data = null, string? member = null) =>
            Write(LogLevel.Warning, message, data, member);

        public void Error(string message, Exception ex, object? data = null, string? member = null) =>
            Write(LogLevel.Error, $"{message}. Exception: {ex.Message}", data, member);

        private void Write(LogLevel level, string message, object? data, string? member)
        {
            var structured = new
            {
                Source = typeof(T).Name,
                Method = member,
                Message = message,
                Data = data,
                CorrelationId = CorrelationIdContext.Current,
                TimestampUtc = DateTime.UtcNow
            };

            if (_logger != null)
            {
                _logger.Log(level, "{@log}", structured);
            }
            else
            {
                Console.WriteLine($"[{level}] {structured.TimestampUtc:o} {structured.Source}.{structured.Method} ({structured.CorrelationId}): {structured.Message} {System.Text.Json.JsonSerializer.Serialize(structured.Data)}");
            }
        }
    }
}