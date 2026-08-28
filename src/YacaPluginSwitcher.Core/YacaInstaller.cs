using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher.Core;

public sealed class YacaInstaller
{
    private readonly BackupManager _backups;
    private readonly Logger _logger;

    public YacaInstaller(BackupManager backups, Logger logger)
    {
        _backups = backups ?? throw new ArgumentNullException(nameof(backups));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Install(
        YacaPluginInfo plugin,
        string targetFile,
        YacaPluginInfo? current,
        bool automaticBackup,
        int maxBackups)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFile);

        var sourceValidation = YacaValidator.Validate(plugin.FilePath);
        if (!sourceValidation.IsValid || sourceValidation.Version is null || string.IsNullOrWhiteSpace(sourceValidation.Sha256))
            throw new YacaOperationException(AppErrorCode.InvalidYacaDll, sourceValidation.Message);

        var targetDirectory = Path.GetDirectoryName(targetFile);
        if (string.IsNullOrWhiteSpace(targetDirectory))
            throw new YacaOperationException(AppErrorCode.TargetDirectoryMissing, "Target directory unavailable.");

        Directory.CreateDirectory(targetDirectory);

        BackupInfo? backup = null;
        if (automaticBackup && File.Exists(targetFile))
        {
            backup = _backups.CreateBackup(targetFile, current);
            if (backup is null)
                throw new YacaOperationException(AppErrorCode.BackupFailed, "Backup creation failed.");
        }

        var temp = Path.Combine(targetDirectory, $".yaca_install_{Guid.NewGuid():N}.tmp");
        var rollbackAttempted = false;
        try
        {
            File.Copy(plugin.FilePath, temp, false);
            var tempValidation = YacaValidator.Validate(temp);
            if (!tempValidation.IsValid || !string.Equals(tempValidation.Sha256, sourceValidation.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new YacaOperationException(AppErrorCode.TemporaryFileVerificationFailed, "Temporary file verification failed.");

            File.Move(temp, targetFile, true);

            var targetValidation = YacaValidator.Validate(targetFile);
            if (!targetValidation.IsValid || !string.Equals(targetValidation.Sha256, sourceValidation.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                if (backup is not null)
                {
                    TryRollback(backup, targetFile);
                    rollbackAttempted = true;
                }

                throw new YacaOperationException(AppErrorCode.InstalledFileVerificationFailed, "Installed target verification failed.");
            }

            _backups.Trim(maxBackups);
            _logger.Info($"YACA aktiviert: {plugin.DisplayName}");
        }
        catch
        {
            if (!rollbackAttempted && backup is not null && File.Exists(targetFile))
            {
                try
                {
                    var targetValidation = YacaValidator.Validate(targetFile);
                    if (!targetValidation.IsValid || !string.Equals(targetValidation.Sha256, sourceValidation.Sha256, StringComparison.OrdinalIgnoreCase))
                        TryRollback(backup, targetFile);
                }
                catch (Exception rollbackException)
                {
                    _logger.Error($"Automatischer Rollback fehlgeschlagen: {rollbackException.Message}");
                }
            }

            throw;
        }
        finally
        {
            TryDelete(temp);
        }
    }

    private void TryRollback(BackupInfo backup, string targetFile)
    {
        var backupFile = Path.Combine(backup.Directory, backup.FileName);
        if (!File.Exists(backupFile))
        {
            _logger.Error("Rollback nicht möglich: Backup-Datei fehlt.");
            return;
        }

        var targetDirectory = Path.GetDirectoryName(targetFile);
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            _logger.Error("Rollback nicht möglich: Zielverzeichnis fehlt.");
            return;
        }

        var rollbackTemp = Path.Combine(targetDirectory, $".yaca_rollback_{Guid.NewGuid():N}.tmp");
        try
        {
            File.Copy(backupFile, rollbackTemp, false);
            File.Move(rollbackTemp, targetFile, true);
            _logger.Warn("Die vorherige YACA-Version wurde automatisch wiederhergestellt.");
        }
        finally
        {
            TryDelete(rollbackTemp);
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
