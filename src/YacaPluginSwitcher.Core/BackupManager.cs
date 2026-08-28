using System.Text.Json;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher.Core;

public sealed class BackupManager
{
    private const string BackupFileName = "yaca_win64.dll";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };
    private readonly string _backupRoot;
    private readonly Logger _logger;

    public BackupManager(string backupRoot, Logger logger)
    {
        _backupRoot = backupRoot ?? throw new ArgumentNullException(nameof(backupRoot));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Directory.CreateDirectory(_backupRoot);
    }

    public BackupInfo? CreateBackup(string targetFile, YacaPluginInfo? current)
    {
        if (string.IsNullOrWhiteSpace(targetFile) || !File.Exists(targetFile))
            return null;

        var timestamp = DateTime.Now;
        var directory = Path.Combine(
            _backupRoot,
            $"{timestamp:yyyy-MM-dd_HHmmss_fff}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        var backupFile = Path.Combine(directory, BackupFileName);
        File.Copy(targetFile, backupFile, false);
        var hash = FileHashService.Sha256(backupFile);
        var fileInfo = new FileInfo(backupFile);
        var info = new BackupInfo(
            directory,
            timestamp,
            BackupFileName,
            current?.DisplayName ?? "Unbekannte YACA-Version",
            fileInfo.Length,
            hash);

        var metadataPath = Path.Combine(directory, "backup.json");
        File.WriteAllText(
            metadataPath,
            JsonSerializer.Serialize(info, JsonOptions));

        _logger.Info($"Backup erstellt: {backupFile}");
        return info;
    }

    public IReadOnlyList<BackupInfo> ListBackups()
    {
        if (!Directory.Exists(_backupRoot))
            return [];

        var list = new List<BackupInfo>();
        IEnumerable<string> directories;
        try
        {
            directories = Directory.EnumerateDirectories(_backupRoot);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Warn($"Backup-Verzeichnis konnte nicht gelesen werden: {ex.Message}");
            return [];
        }

        foreach (var directory in directories)
        {
            var metadataPath = Path.Combine(directory, "backup.json");
            var backupFile = Path.Combine(directory, BackupFileName);
            try
            {
                if (!File.Exists(metadataPath) || !File.Exists(backupFile))
                    continue;

                var info = JsonSerializer.Deserialize<BackupInfo>(File.ReadAllText(metadataPath));
                if (info is not null &&
                    string.Equals(Path.GetFullPath(info.Directory), Path.GetFullPath(directory), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(info.FileName, BackupFileName, StringComparison.OrdinalIgnoreCase) &&
                    info.FileSize > 0 &&
                    !string.IsNullOrWhiteSpace(info.Sha256))
                {
                    list.Add(info);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                _logger.Warn($"Backup-Metadaten ignoriert: {metadataPath} -> {ex.Message}");
            }
        }

        return list.OrderByDescending(backup => backup.Timestamp).ToList();
    }

    public void Restore(BackupInfo backup, string targetFile)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFile);

        var backupFile = Path.Combine(backup.Directory, backup.FileName);
        if (!File.Exists(backupFile))
            throw new YacaOperationException(AppErrorCode.BackupFileMissing, "Backup file missing.");

        var validation = YacaValidator.Validate(backupFile);
        if (!validation.IsValid)
            throw new YacaOperationException(AppErrorCode.BackupInvalid, validation.Message);

        if (!string.Equals(validation.Sha256, backup.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new YacaOperationException(AppErrorCode.BackupHashMismatch, "Backup hash verification failed.");

        var targetDirectory = Path.GetDirectoryName(targetFile);
        if (string.IsNullOrWhiteSpace(targetDirectory))
            throw new YacaOperationException(AppErrorCode.BackupTargetDirectoryMissing, "Target directory unavailable.");

        Directory.CreateDirectory(targetDirectory);
        var temp = Path.Combine(targetDirectory, $".yaca_restore_{Guid.NewGuid():N}.tmp");
        try
        {
            File.Copy(backupFile, temp, false);
            File.Move(temp, targetFile, true);
            _logger.Info($"Backup wiederhergestellt: {backup.DisplayName}");
        }
        finally
        {
            TryDelete(temp);
        }
    }


    public void DeleteBackups(IEnumerable<BackupInfo> backups)
    {
        ArgumentNullException.ThrowIfNull(backups);
        var root = Path.GetFullPath(_backupRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (var backup in backups)
        {
            ArgumentNullException.ThrowIfNull(backup);
            var directory = Path.GetFullPath(backup.Directory);
            if (!directory.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new YacaOperationException(AppErrorCode.InvalidBackupDirectory, "Backup directory outside configured root.");
            if (!Directory.Exists(directory))
                continue;
            Directory.Delete(directory, true);
            _logger.Info($"Backup gelöscht: {directory}");
        }
    }

    public void Trim(int maxBackups)
    {
        maxBackups = Math.Max(1, maxBackups);
        foreach (var backup in ListBackups().Skip(maxBackups))
        {
            try
            {
                Directory.Delete(backup.Directory, true);
                _logger.Info($"Altes Backup gelöscht: {backup.Directory}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.Warn($"Backup konnte nicht gelöscht werden: {ex.Message}");
            }
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
}
