using System.Net.Http;

namespace YacaPluginSwitcher.Core;

public sealed partial class YacaUpdaterService
{
    /// <summary>
    /// Lädt ausschließlich die vom Benutzer ausgewählten Versionen herunter.
    /// Die Auswahl wird vor dem Download nochmals gegen den aktuell fehlenden
    /// Versionsbestand geprüft, damit keine bereits installierte Version unnötig
    /// überschrieben wird.
    /// </summary>
    public async Task DownloadSelectedAsync(
        IEnumerable<string> selectedVersions,
        IProgress<YacaUpdaterProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selectedVersions);

        Directory.CreateDirectory(DownloadDirectory);
        Directory.CreateDirectory(TempDirectory);

        CleanTemporaryUpdaterFiles();
        await ProcessExistingDownloadsAsync(progress, cancellationToken);

        var requestedVersions = selectedVersions
            .Where(version => !string.IsNullOrWhiteSpace(version))
            .Select(version => version.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requestedVersions.Count == 0)
            return;

        var stillMissing = await GetMissingVersionsAsync(cancellationToken);
        var missingSet = new HashSet<string>(stillMissing, StringComparer.OrdinalIgnoreCase);

        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        foreach (var version in requestedVersions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!missingSet.Contains(version))
                continue;

            await DownloadVersionAsync(
                http,
                version,
                progress,
                cancellationToken);
        }
    }
}
