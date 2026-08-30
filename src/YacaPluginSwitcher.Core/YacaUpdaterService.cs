using System.Globalization;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher.Core;

/// <summary>
/// Beschreibt den Fortschritt eines einzelnen YACA-Updates.
/// </summary>
public sealed record YacaUpdaterProgress(
    string Version,
    long BytesReceived,
    long? TotalBytes,
    string Status,
    bool Completed,
    bool Success,
    string? Error);

/// <summary>
/// Verantwortlich für das Ermitteln, Herunterladen, Validieren und Integrieren
/// von YACA-Versionen. Temporäre Dateien werden ausschließlich im portablen
/// Temp-Verzeichnis neben der Anwendung verarbeitet.
/// </summary>
public sealed class YacaUpdaterService
{
    private const string CdnBase = "https://cdn.yaca.systems/yaca_{0}_3.6.x.ts3_plugin";

    private static readonly CompositeFormat CdnFormat = CompositeFormat.Parse(CdnBase);
    private static readonly Version MinExclusiveVersion = new(1, 7, 5);

    private static readonly Regex ArchiveDllRegex = new(
        @"^yaca(?:_\d+)?_win64\.dll$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex VersionArchiveRegex = new(
        @"yaca_(\d+)(?:_3\.6\.x)?\.ts3_plugin",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex VersionDigitsRegex = new(
        @"^yaca_(\d+)_win64\.dll$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex VersionStringRegex = new(
        @"^\d+\.\d+\.\d+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly YacaService _service;

    public YacaUpdaterService(YacaService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>
    /// Verzeichnis für dauerhaft gespeicherte TS3-Plugin-Archive.
    /// </summary>
    public string DownloadDirectory =>
        Path.Combine(_service.Paths.BaseDirectory, "plugins_download");

    /// <summary>
    /// Portables Arbeitsverzeichnis für Downloads, Extraktion und Validierung.
    /// </summary>
    public string TempDirectory => _service.Paths.TempDirectory;

    /// <summary>
    /// Liest die bereits gespeicherten TS3-Plugin-Archive aus dem Downloadverzeichnis.
    /// </summary>
    public Task<IReadOnlyList<(string Version, string FileName, long Size)>>
        GetAvailableDownloadsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(DownloadDirectory);

        var result = Directory
            .EnumerateFiles(
                DownloadDirectory,
                "*.ts3_plugin",
                SearchOption.TopDirectoryOnly)
            .Select(path =>
            (
                Version: ExtractVersion(Path.GetFileName(path)) ?? "—",
                FileName: Path.GetFileName(path),
                Size: new FileInfo(path).Length
            ))
            .OrderByDescending(item => ParseVersion(item.Version))
            .ToList();

        return Task.FromResult<IReadOnlyList<(string Version, string FileName, long Size)>>(result);
    }

    /// <summary>
    /// Ermittelt alle online verfügbaren Versionen, für die noch keine
    /// erfolgreich validierte DLL im lokalen Versionsbestand existiert.
    /// Ein TS3-Plugin-Archiv allein gilt niemals als installierte Version.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetMissingVersionsAsync(
        CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        var versions = new SortedDictionary<long, string>();

        await LoadVersionsAsync(
            http,
            "https://yaca.systems/api/changelog/getDownloads",
            versions,
            cancellationToken);

        await LoadVersionsAsync(
            http,
            "https://yaca.systems/api/changelog/get?locale=en",
            versions,
            cancellationToken);

        var localVersions = GetValidatedLocalVersions();

        return versions
            .Where(item =>
                Version.TryParse(item.Value, out var version)
                && version > MinExclusiveVersion
                && !localVersions.Contains(
                    item.Value.Replace(".", "", StringComparison.Ordinal)))
            .Select(item => item.Value)
            .OrderByDescending(ParseVersion)
            .ToList();
    }

    /// <summary>
    /// Prüft beim ersten Aufruf bereits gespeicherte Archive und integriert
    /// ausschließlich erfolgreich validierte Versionen in den Versionsbestand.
    /// </summary>
    public Task ProcessStoredDownloadsAsync(
        IProgress<YacaUpdaterProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return ProcessExistingDownloadsAsync(progress, cancellationToken);
    }

    /// <summary>
    /// Lädt alle fehlenden Versionen herunter und integriert sie nach vollständiger
    /// Validierung in den lokalen Versionsbestand.
    /// </summary>
    public async Task DownloadMissingAsync(
        IProgress<YacaUpdaterProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(DownloadDirectory);
        Directory.CreateDirectory(TempDirectory);

        CleanTemporaryUpdaterFiles();
        await ProcessExistingDownloadsAsync(progress, cancellationToken);

        var missingVersions = await GetMissingVersionsAsync(cancellationToken);

        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        foreach (var version in missingVersions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await DownloadVersionAsync(http, version, progress, cancellationToken);
        }
    }

    /// <summary>
    /// Lädt eine einzelne fehlende Version in das temporäre Arbeitsverzeichnis.
    /// Erst nach erfolgreicher Verarbeitung wird das Archiv dauerhaft gespeichert
    /// oder verworfen.
    /// </summary>
    private async Task DownloadVersionAsync(
        HttpClient http,
        string version,
        IProgress<YacaUpdaterProgress>? progress,
        CancellationToken cancellationToken)
    {
        var tag = version.Replace(".", "", StringComparison.Ordinal);
        var archivePath = Path.Combine(
            TempDirectory,
            $"yaca_{tag}_3.6.x.ts3_plugin");
        var partialPath = archivePath + ".part";

        try
        {
            TryDelete(archivePath);
            TryDelete(partialPath);

            Report(progress, version, "Download", false, false, null);

            using var response = await http.GetAsync(
                string.Format(CultureInfo.InvariantCulture, CdnFormat, version),
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;

            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = File.Create(partialPath);

            var buffer = new byte[81920];
            long bytesReceived = 0;
            int bytesRead;

            while ((bytesRead = await input.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                bytesReceived += bytesRead;

                progress?.Report(
                    new YacaUpdaterProgress(
                        version,
                        bytesReceived,
                        totalBytes,
                        "Download",
                        false,
                        false,
                        null));
            }

            await output.FlushAsync(cancellationToken);
            output.Close();

            File.Move(partialPath, archivePath, true);

            await ProcessDownloadedArchiveAsync(
                version,
                archivePath,
                progress,
                cancellationToken);

            FinalizeDownloadedArchive(archivePath);
        }
        catch (OperationCanceledException)
        {
            TryDelete(partialPath);
            TryDelete(archivePath);
            throw;
        }
        catch (Exception ex) when (
            ex is HttpRequestException
            or IOException
            or InvalidDataException
            or UnauthorizedAccessException
            or InvalidOperationException)
        {
            TryDelete(partialPath);
            TryDelete(archivePath);

            progress?.Report(
                new YacaUpdaterProgress(
                    version,
                    0,
                    null,
                    "Fehlgeschlagen",
                    true,
                    false,
                    ex.Message));
        }
    }

    /// <summary>
    /// Verschiebt ein erfolgreich verarbeitetes Archiv in den dauerhaften
    /// Downloadbestand oder löscht es, wenn die Speicherung deaktiviert ist.
    /// </summary>
    private void FinalizeDownloadedArchive(string archivePath)
    {
        if (_service.Settings.KeepYacaPluginDownloads)
        {
            Directory.CreateDirectory(DownloadDirectory);

            var destination = Path.Combine(
                DownloadDirectory,
                Path.GetFileName(archivePath));

            File.Move(archivePath, destination, true);
            return;
        }

        TryDelete(archivePath);
    }

    /// <summary>
    /// Verarbeitet bereits vorhandene Archive aus plugins_download.
    /// Ein vorhandenes Ziel wird nur dann als bereits integriert betrachtet,
    /// wenn die DLL erneut erfolgreich validiert werden kann.
    /// </summary>
    private async Task ProcessExistingDownloadsAsync(
        IProgress<YacaUpdaterProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(DownloadDirectory))
            return;

        var archives = Directory
            .EnumerateFiles(
                DownloadDirectory,
                "yaca_*.ts3_plugin",
                SearchOption.TopDirectoryOnly)
            .Select(path =>
            (
                Path: path,
                Version: ExtractVersion(Path.GetFileName(path))
            ))
            .Where(item => !string.IsNullOrWhiteSpace(item.Version))
            .OrderByDescending(item => ParseVersion(item.Version!))
            .ToList();

        foreach (var archive in archives)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var version = archive.Version!;
            var tag = version.Replace(".", "", StringComparison.Ordinal);
            var target = Path.Combine(
                _service.Paths.PluginDirectory,
                $"yaca_{tag}_win64.dll");

            if (IsValidInstalledVersion(target, version))
            {
                if (!_service.Settings.KeepYacaPluginDownloads)
                    TryDelete(archive.Path);

                continue;
            }

            try
            {
                await ProcessDownloadedArchiveAsync(
                    version,
                    archive.Path,
                    progress,
                    cancellationToken);

                if (!_service.Settings.KeepYacaPluginDownloads)
                    TryDelete(archive.Path);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (
                ex is IOException
                or InvalidDataException
                or UnauthorizedAccessException
                or InvalidOperationException)
            {
                progress?.Report(
                    new YacaUpdaterProgress(
                        version,
                        0,
                        null,
                        "Fehlgeschlagen",
                        true,
                        false,
                        ex.Message));
            }
        }
    }

    /// <summary>
    /// Verarbeitet ein TS3-Plugin-Archiv vollständig im Temp-Verzeichnis.
    /// Die DLL wird extrahiert, validiert, gestaged und anschließend nochmals
    /// anhand von SHA-256 und Version geprüft.
    /// </summary>
    private Task ProcessDownloadedArchiveAsync(
        string version,
        string archivePath,
        IProgress<YacaUpdaterProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException(
                "YACA-Download nicht gefunden.",
                archivePath);
        }

        var tag = version.Replace(".", "", StringComparison.Ordinal);
        var target = Path.Combine(
            _service.Paths.PluginDirectory,
            $"yaca_{tag}_win64.dll");

        var extractionDirectory = Path.Combine(
            TempDirectory,
            "validate_" + Guid.NewGuid().ToString("N"));

        var tempDll = Path.Combine(
            extractionDirectory,
            "yaca_win64.dll");

        var tempTarget = Path.Combine(
            _service.Paths.PluginDirectory,
            $".yaca_install_{Guid.NewGuid():N}.tmp");

        try
        {
            Report(progress, version, "Extraktion", false, false, null);

            Directory.CreateDirectory(extractionDirectory);
            ExtractYacaDll(archivePath, tempDll);

            Report(progress, version, "Prüfung", false, false, null);
            EnsureExtractedDllExists(tempDll);

            Report(progress, version, "Validierung", false, false, null);
            var validation = YacaValidator.Validate(tempDll);

            EnsureValidYacaVersion(validation, version);

            Directory.CreateDirectory(_service.Paths.PluginDirectory);

            Report(progress, version, "Verschieben", false, false, null);
            File.Copy(tempDll, tempTarget, false);

            ValidateStagedDll(tempTarget, validation, version);

            File.Move(tempTarget, target, true);
            ValidateInstalledDll(target, validation, version);

            Report(
                progress,
                version,
                _service.Settings.KeepYacaPluginDownloads
                    ? "Download behalten"
                    : "Download löschen",
                false,
                false,
                null);

            Report(progress, version, "Abgeschlossen", true, true, null);
        }
        finally
        {
            TryDelete(tempDll);
            TryDelete(tempTarget);
            TryDeleteDirectory(extractionDirectory);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Extrahiert ausschließlich eine passende YACA-Win64-DLL aus dem Archiv.
    /// </summary>
    private static void ExtractYacaDll(string archivePath, string destinationPath)
    {
        using var zip = ZipFile.OpenRead(archivePath);

        var entry = zip.Entries.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(item.Name)
            && ArchiveDllRegex.IsMatch(Path.GetFileName(item.FullName)));

        if (entry is null)
        {
            throw new InvalidDataException(
                "Keine YACA x64-DLL im TS3-Plugin-Archiv gefunden.");
        }

        entry.ExtractToFile(destinationPath, true);
    }

    /// <summary>
    /// Stellt sicher, dass die extrahierte DLL tatsächlich vorhanden und nicht leer ist.
    /// </summary>
    private static void EnsureExtractedDllExists(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length == 0)
        {
            throw new InvalidDataException(
                "Die extrahierte DLL ist leer oder wurde nicht erstellt.");
        }
    }

    /// <summary>
    /// Prüft die extrahierte DLL auf Gültigkeit, erkannte Version und SHA-256.
    /// </summary>
    private static void EnsureValidYacaVersion(
        YacaValidationResult validation,
        string expectedVersion)
    {
        if (!validation.IsValid
            || validation.Version is null
            || string.IsNullOrWhiteSpace(validation.Sha256))
        {
            throw new InvalidDataException(
                $"Die extrahierte DLL ist ungültig: {validation.Message}");
        }

        if (!Version.TryParse(expectedVersion, out var expected)
            || validation.Version != expected)
        {
            throw new InvalidDataException(
                $"Versionsprüfung fehlgeschlagen: erwartet {expectedVersion}, erkannt {validation.Version}.");
        }
    }

    /// <summary>
    /// Validiert die temporär im Plugin-Verzeichnis bereitgestellte DLL und
    /// vergleicht sie mit dem Ergebnis der ursprünglichen Validierung.
    /// </summary>
    private static void ValidateStagedDll(
        string path,
        YacaValidationResult sourceValidation,
        string expectedVersion)
    {
        var stagedValidation = YacaValidator.Validate(path);

        if (!stagedValidation.IsValid
            || !string.Equals(
                stagedValidation.Sha256,
                sourceValidation.Sha256,
                StringComparison.OrdinalIgnoreCase)
            || stagedValidation.Version is null
            || !Version.TryParse(expectedVersion, out var expected)
            || stagedValidation.Version != expected)
        {
            throw new InvalidDataException(
                "Die temporär bereitgestellte DLL konnte nicht verifiziert werden.");
        }
    }

    /// <summary>
    /// Validiert die final installierte DLL nochmals gegen die ursprüngliche Quelle.
    /// </summary>
    private static void ValidateInstalledDll(
        string path,
        YacaValidationResult sourceValidation,
        string expectedVersion)
    {
        var installedValidation = YacaValidator.Validate(path);

        if (!installedValidation.IsValid
            || !string.Equals(
                installedValidation.Sha256,
                sourceValidation.Sha256,
                StringComparison.OrdinalIgnoreCase)
            || installedValidation.Version is null
            || !Version.TryParse(expectedVersion, out var expected)
            || installedValidation.Version != expected)
        {
            throw new InvalidDataException(
                "Die verschobene DLL konnte nicht erfolgreich verifiziert werden.");
        }
    }

    /// <summary>
    /// Ermittelt ausschließlich lokal vorhandene und erfolgreich validierte
    /// Versions-DLLs.
    /// </summary>
    private HashSet<string> GetValidatedLocalVersions()
    {
        var versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(_service.Paths.PluginDirectory))
            return versions;

        foreach (var file in Directory.EnumerateFiles(
                     _service.Paths.PluginDirectory,
                     "yaca_*_win64.dll",
                     SearchOption.TopDirectoryOnly))
        {
            var match = VersionDigitsRegex.Match(Path.GetFileName(file));

            if (!match.Success)
                continue;

            var digits = match.Groups[1].Value;

            if (digits.Length != 3
                || !Version.TryParse(
                    $"{digits[0]}.{digits[1]}.{digits[2]}",
                    out var expected))
            {
                continue;
            }

            var validation = YacaValidator.Validate(file);

            if (validation.IsValid
                && validation.Version is not null
                && validation.Version == expected)
            {
                versions.Add(digits);
            }
        }

        return versions;
    }

    /// <summary>
    /// Prüft, ob die angegebene Ziel-DLL bereits als gültige Version installiert ist.
    /// </summary>
    private static bool IsValidInstalledVersion(string target, string expectedVersion)
    {
        if (!File.Exists(target))
            return false;

        var validation = YacaValidator.Validate(target);

        return validation.IsValid
               && validation.Version is not null
               && validation.Version.Equals(ParseVersion(expectedVersion));
    }

    /// <summary>
    /// Entfernt alte temporäre Updater-Dateien und Validierungsverzeichnisse.
    /// </summary>
    private void CleanTemporaryUpdaterFiles()
    {
        if (!Directory.Exists(TempDirectory))
            return;

        foreach (var file in Directory.EnumerateFiles(
                     TempDirectory,
                     "yaca_*.ts3_plugin*",
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

    /// <summary>
    /// Meldet einen Status ohne zusätzliche Byte-Fortschrittsinformation.
    /// </summary>
    private static void Report(
        IProgress<YacaUpdaterProgress>? progress,
        string version,
        string status,
        bool completed,
        bool success,
        string? error)
    {
        progress?.Report(
            new YacaUpdaterProgress(
                version,
                0,
                null,
                status,
                completed,
                success,
                error));
    }

    /// <summary>
    /// Lädt Versionen aus einer YACA-API und ignoriert ausschließlich erwartbare
    /// HTTP- bzw. JSON-Fehler, damit die zweite Quelle weiterhin abgefragt werden kann.
    /// </summary>
    private static async Task LoadVersionsAsync(
        HttpClient http,
        string url,
        SortedDictionary<long, string> versions,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await http.GetStreamAsync(
                url,
                cancellationToken);

            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return;

            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (!element.TryGetProperty("version", out var property))
                    continue;

                var version = property.GetString();

                if (string.IsNullOrWhiteSpace(version)
                    || !VersionStringRegex.IsMatch(version))
                {
                    continue;
                }

                if (long.TryParse(
                    version.Replace(".", "", StringComparison.Ordinal),
                    out var number))
                {
                    versions[number] = version;
                }
            }
        }
        catch (HttpRequestException)
        {
            // Eine nicht erreichbare Quelle darf die Prüfung der zweiten Quelle nicht verhindern.
        }
        catch (JsonException)
        {
            // Ungültige API-Daten werden verworfen; die zweite Quelle wird weiterhin geprüft.
        }
    }

    private static Version ParseVersion(string value)
    {
        return Version.TryParse(value, out var version)
            ? version
            : new Version(0, 0, 0);
    }

    /// <summary>
    /// Extrahiert die dreistellige Versionsnummer aus einem gespeicherten Archivnamen.
    /// </summary>
    private static string? ExtractVersion(string fileName)
    {
        var match = VersionArchiveRegex.Match(fileName);

        if (!match.Success)
            return null;

        var digits = match.Groups[1].Value;

        return digits.Length == 3
            ? $"{digits[0]}.{digits[1]}.{digits[2]}"
            : null;
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
            // Temporäre Aufräumfehler dürfen den eigentlichen Updateablauf nicht blockieren.
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
            // Temporäre Aufräumfehler dürfen den eigentlichen Updateablauf nicht blockieren.
        }
    }
}
