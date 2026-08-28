namespace YacaPluginSwitcher.Core;

public enum AppErrorCode
{
    InvalidYacaDll,
    TargetDirectoryMissing,
    BackupFailed,
    TemporaryFileVerificationFailed,
    InstalledFileVerificationFailed,
    BackupFileMissing,
    BackupInvalid,
    BackupHashMismatch,
    BackupTargetDirectoryMissing,
    ConfigurationDirectoryMissing,
    InvalidBackupDirectory
}

public sealed class YacaOperationException : Exception
{
    public YacaOperationException(AppErrorCode code, string technicalMessage, Exception? innerException = null)
        : base(technicalMessage, innerException)
    {
        Code = code;
    }

    public AppErrorCode Code { get; }
}
