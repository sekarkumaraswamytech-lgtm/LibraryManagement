namespace LibrarySystem.Application.Middleware
{
    /// <summary>
    /// Async-local holder for current correlation id accessible to StructuredLogger.
    /// </summary>
    public static class CorrelationIdContext
    {
        private static readonly AsyncLocal<string?> _current = new();
        public static string? Current => _current.Value;
        public static void Set(string? id) => _current.Value = id;
        public static void Clear() => _current.Value = null;
    }
}