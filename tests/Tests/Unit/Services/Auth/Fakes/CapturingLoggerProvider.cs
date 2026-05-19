using Microsoft.Extensions.Logging;

namespace Tests.Unit.Services.Auth.Fakes;

/// <summary>
/// Manual fake <see cref="ILoggerProvider"/> that records every log
/// call on a thread-safe list. Used by FR-008 tests (#71 slice 7) to
/// assert that swallowed exceptions in
/// <c>RegistrationEndpoints.Register</c> are logged at error level
/// with the exception object attached.
/// </summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly object _lock = new();
    private readonly List<CapturedLogEntry> _entries = new();

    public IReadOnlyList<CapturedLogEntry> Entries
    {
        get
        {
            lock (_lock) { return _entries.ToList(); }
        }
    }

    public ILogger CreateLogger(string categoryName)
        => new CapturingLogger(categoryName, this);

    public void Dispose() { }

    internal void Capture(CapturedLogEntry entry)
    {
        lock (_lock) { _entries.Add(entry); }
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly string _category;
        private readonly CapturingLoggerProvider _provider;

        public CapturingLogger(string category, CapturingLoggerProvider provider)
        {
            _category = category;
            _provider = provider;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            _provider.Capture(new CapturedLogEntry(_category, logLevel, eventId,
                exception, message));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}

internal sealed record CapturedLogEntry(string Category, LogLevel Level,
    EventId EventId, Exception? Exception, string Message);
