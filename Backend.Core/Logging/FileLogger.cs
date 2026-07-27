using Microsoft.Extensions.Logging;

namespace Backend.Core.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private static readonly object _lock = new();

        public FileLogger(string baseDirectory)
        {
            string logsDirectory = Path.Combine(baseDirectory, "Logs");
            Directory.CreateDirectory(logsDirectory);

            // Cleanup old logs (older than 30 days)
            foreach (var file in Directory.GetFiles(logsDirectory, "log_*.txt"))
            {
                var creationTime = File.GetCreationTime(file);
                if (creationTime < DateTime.Now.AddDays(-30))
                {
                    try { File.Delete(file); } catch { /* ignore */ }
                }
            }

            // Unique file per app run
            string fileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            _filePath = Path.Combine(logsDirectory, fileName);
        }

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            string message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {formatter(state, exception)}";
            if (exception != null)
                message += Environment.NewLine + exception;

            lock (_lock)
            {
                File.AppendAllText(_filePath, message + Environment.NewLine);
            }
        }
    }

    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _baseDirectory;

        public FileLoggerProvider(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }

        public ILogger CreateLogger(string categoryName) => new FileLogger(_baseDirectory);

        public void Dispose() { }
    }
}