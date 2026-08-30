using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher.Core;

public sealed partial class YacaUpdaterService
{
    /// <summary>
    /// Lädt ausschließlich die vom Benutzer ausgewählten fehlenden Versionen.
    /// Bereits gespeicherte Archive werden vorher über denselben Validierungspfad verarbeitet.
    /// </summary>
    public async Task DownloadSelectedAsync(
        IReadOnlyCollection<string> selectedVersions,
        IProgress<YacaUpdaterProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selectedVersions);

        Directory.CreateDirectory(DownloadDirectory);
        Directory.CreateDirectory(TempDirectory);

        CleanTemporaryUpdaterFiles();
        await ProcessExistingDownloadsAsync(progress, cancellationToken);

        var requestedVersions = new HashSet<string>(
            selectedVersions
                .Where(IsSupportedVersion)
                .Select(NormalizeVersion),
            StringComparer.OrdinalIgnoreCase);

        if (requestedVersions.Count == 0)
            return;

        var missingVersions = await GetMissingVersionsAsync(cancellationToken);

        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        foreach (var version in missingVersions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!requestedVersions.Contains(NormalizeVersion(version)))
                continue;

            await DownloadVersionAsync(
                http,
                version,
                progress,
                cancellationToken);
        }
    }

    /// <summary>
    /// Prüft, ob eine vom UI übergebene Versionsnummer dem unterstützten Format entspricht.
    /// </summary>
    private static bool IsSupportedVersion(string version)
    {
        return !string.IsNullOrWhiteSpace(version)
               && Version.TryParse(version, out var parsed)
               && parsed > MinExclusiveVersion;
    }

    /// <summary>
    /// Vereinheitlicht eine Versionsnummer für Vergleiche zwischen UI und Updater.
    /// </summary>
    private static string NormalizeVersion(string version)
    {
        return Version.TryParse(version, out var parsed)
            ? parsed.ToString(3)
            : version.Trim();
    }
}
