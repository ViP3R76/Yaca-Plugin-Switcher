namespace YacaPluginSwitcher.Core;

public sealed partial class YacaUpdaterService
{
    private static string? DecodeCompactVersion(string digits)
    {
        if (digits.Length == 3)
            return $"{digits[0]}.{digits[1]}.{digits[2]}";

        if (digits.Length == 6)
            return $"{digits[..2]}.{digits[2..4]}.{digits[4..6]}";

        return null;
    }

    private static long ParseVersion(string version)
    {
        if (!Version.TryParse(version, out var parsed))
            return long.MinValue;

        return parsed.Major * 1_000_000L + parsed.Minor * 1_000L + parsed.Build;
    }

    private static void Report(
        IProgress<YacaUpdaterProgress>? progress,
        string version,
        string status,
        bool completed,
        bool success,
        string? error)
    {
        progress?.Report(new YacaUpdaterProgress(version, 0, null, status, completed, success, error));
    }

    private void CleanTemporaryUpdaterFiles()
    {
        if (!Directory.Exists(TempDirectory))
            return;

        foreach (var file in Directory.EnumerateFiles(
                     TempDirectory,
                     "yaca_*_3.6.x.ts3_plugin*",
                     SearchOption.TopDirectoryOnly))
        {
            TryDelete(file);
        }

        foreach (var directory in Directory.EnumerateDirectories(
                     TempDirectory,
                     "validate_*",
                     SearchOption.TopDirectoryOnly))
        {
            TryDeleteDirectory(directory);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Cleanup must not mask the original exception.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch
        {
            // Cleanup must not mask the original exception.
        }
    }
}
