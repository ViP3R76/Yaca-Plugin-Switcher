using System.Globalization;

namespace YacaPluginSwitcher.Core;

public sealed class Logger
{
    private const int LogRetentionDays = 3;
    private const string LogFilePrefix = "YacaPluginSwitcher-";
    private const string LogFileExtension = ".log";
    private const string LegacyLogFileName = "YacaPluginSwitcher.log";

    private readonly string _directory;
    private readonly object _sync = new();
    private volatile bool _generalLogging;
    private volatile bool _debugLogging;

    public Logger(string directory, bool generalLogging, bool debugLogging)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        _directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(_directory);
        CleanupOldLogs(DateTime.UtcNow);

        _generalLogging = generalLogging;
        _debugLogging = debugLogging;
    }

    public string FilePath => GetLogFilePath(DateTime.Now);

    public void Configure(bool generalLogging, bool debugLogging)
    {
        _generalLogging = generalLogging;
        _debugLogging = debugLogging;
    }

    public int DeleteLogs()
    {
        lock (_sync)
        {
            var root = Path.GetFullPath(_directory);
            var deleted = 0;

            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly))
            {
                if (!IsOwnedLogFile(file, root))
                    continue;

                try
                {
                    File.Delete(file);
                    deleted++;
                }
                catch (IOException)
                {
                    // A log may be locked by another process; leave it untouched.
                }
                catch (UnauthorizedAccessException)
                {
                    // A log may not be deletable; leave it untouched.
                }
            }

            return deleted;
        }
    }

    public void Info(string message)
    {
        if (_generalLogging)
            Write("INFO", message);
    }

    public void Debug(string message)
    {
        if (_debugLogging)
            Write("DEBUG", message);
    }

    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        try
        {
            lock (_sync)
            {
                var now = DateTime.Now;
                File.AppendAllText(
                    GetLogFilePath(now),
                    $"[{now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}] [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never crash the application.
        }
    }

    private string GetLogFilePath(DateTime timestamp)
    {
        var fileName = $"{LogFilePrefix}{timestamp:yyyy-MM-dd}{LogFileExtension}";
        return Path.Combine(_directory, fileName);
    }

    private static bool IsOwnedLogFile(string filePath, string root)
    {
        var fullPath = Path.GetFullPath(filePath);
        var relative = Path.GetRelativePath(root, fullPath);

        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            return false;

        var fileName = Path.GetFileName(fullPath);
        return fileName.StartsWith(LogFilePrefix, StringComparison.Ordinal)
               && fileName.EndsWith(LogFileExtension, StringComparison.Ordinal)
               && fileName.Length == LogFilePrefix.Length + 10 + LogFileExtension.Length
               && DateTime.TryParseExact(
                   fileName.AsSpan(LogFilePrefix.Length, 10),
                   "yyyy-MM-dd",
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out _)
               || string.Equals(fileName, LegacyLogFileName, StringComparison.Ordinal);
    }

    private void CleanupOldLogs(DateTime utcNow)
    {
        try
        {
            var cutoffUtc = utcNow.AddDays(-LogRetentionDays);
            var root = Path.GetFullPath(_directory);

            foreach (var file in Directory.EnumerateFiles(root, $"{LogFilePrefix}*{LogFileExtension}", SearchOption.TopDirectoryOnly))
            {
                DeleteIfOlderThan(file, cutoffUtc, root);
            }

            // Remove the pre-daily-log legacy file as well, but only from this exact log directory.
            var legacyFile = Path.Combine(root, LegacyLogFileName);
            if (File.Exists(legacyFile))
                DeleteIfOlderThan(legacyFile, cutoffUtc, root);
        }
        catch
        {
            // Log cleanup must never prevent application startup.
        }
    }

    private static void DeleteIfOlderThan(string filePath, DateTime cutoffUtc, string root)
    {
        var fullPath = Path.GetFullPath(filePath);
        var relative = Path.GetRelativePath(root, fullPath);

        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            return;

        if (File.GetLastWriteTimeUtc(fullPath) < cutoffUtc)
            File.Delete(fullPath);
    }
}
