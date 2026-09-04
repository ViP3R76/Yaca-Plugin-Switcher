using System.Globalization;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher.Core;

public sealed record YacaUpdaterProgress(
    string Version,
    long BytesReceived,
    long? TotalBytes,
    string Status,
    bool Completed,
    bool Success,
    string? Error);

/// <summary>
/// Verantwortlich für Ermittlung, Download, Validierung und Integration von YACA-Versionen.
/// Temporäre Verarbeitung erfolgt ausschließlich im portablen Temp-Verzeichnis.
/// </summary>
public sealed partial class YacaUpdaterService
{
    private const string CdnBase = "https://cdn.yaca.systems/yaca_{0}_3.6.x.ts3_plugin";
    private static readonly CompositeFormat CdnFormat = CompositeFormat.Parse(CdnBase);
    private static readonly Version MinExclusiveVersion = new(1, 7, 5);

    private static readonly Regex ArchiveDllRegex = new(
        @"^yaca(?:_\d+)?_win64\.dll$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    // Aktuelles Format: yaca_1.8.2_3.6.x.ts3_plugin. Das intern erzeugte Legacy-Format yaca_182_3.6.x.ts3_plugin bleibt ebenfalls lesbar.
    private static readonly Regex VersionArchiveRegex = new(
        @"^yaca_(\d+\.\d+\.\d+|\d{3}|\d{6})(?:_3\.6\.x)?\.ts3_plugin$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex VersionDigitsRegex = new(
        @"^yaca_(\d{3}|\d{6})_win64\.dll$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex VersionStringRegex = new(
        @"^\d+\.\d+\.\d+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly YacaService _service;

    public YacaUpdaterService(YacaService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public string DownloadDirectory => Path.Combine(_service.Paths.BaseDirectory, "plugins_download");
    public string TempDirectory => _service.Paths.TempDirectory;

    public Task<IReadOnlyList<(string Version, string FileName, long Size)>> GetAvailableDownloadsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(DownloadDirectory);

        var result = Directory
            .EnumerateFiles(DownloadDirectory, "*.ts3_plugin", SearchOption.TopDirectoryOnly)
            .Select(path =>
            {
                var version = ExtractVersion(Path.GetFileName(path));
                return new
                {
                    Version = version,
                    FileName = Path.GetFileName(path),
                    Size = new FileInfo(path).Length
                };
            })
            .Where(item => item.Version is not null)
            .Select(item => (Version: item.Version!, item.FileName, item.Size))
            .OrderByDescending(item => ParseVersion(item.Version))
            .ToList();

        return Task.FromResult<IReadOnlyList<(string Version, string FileName, long Size)>>(result);
    }

    public async Task<IReadOnlyList<string>> GetMissingVersionsAsync(CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var versions = new SortedDictionary<long, string>();

        await LoadVersionsAsync(http, "https://yaca.systems/api/changelog/getDownloads", versions, cancellationToken);
        await LoadVersionsAsync(http, "https://yaca.systems/api/changelog/get?locale=en", versions, cancellationToken);

        var localVersions = GetValidatedLocalVersions();

        return versions
            .Where(item => Version.TryParse(item.Value, out var version)
                           && version > MinExclusiveVersion
                           && !localVersions.Contains(item.Value.Replace(".", "", StringComparison.Ordinal)))
            .Select(item => item.Value)
            .OrderByDescending(ParseVersion)
            .ToList();
    }

    public Task ProcessStoredDownloadsAsync(
        IProgress<YacaUpdaterProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => ProcessExistingDownloadsAsync(progress, cancellationToken);

    public async Task DownloadMissingAsync(
        IProgress<YacaUpdaterProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(DownloadDirectory);
        Directory.CreateDirectory(TempDirectory);
        CleanTemporaryUpdaterFiles();
        await ProcessExistingDownloadsAsync(progress, cancellationToken);

        var missingVersions = await GetMissingVersionsAsync(cancellationToken);
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

        foreach (var version in missingVersions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await DownloadVersionAsync(http, version, progress, cancellationToken);
        }
    }

    private async Task DownloadVersionAsync(
        HttpClient http,
        string version,
        IProgress<YacaUpdaterProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!VersionStringRegex.IsMatch(version))
            throw new InvalidDataException($"Ungültige YACA-Version: {version}");

        var tag = version.Replace(".", "", StringComparison.Ordinal);
        var archivePath = Path.Combine(TempDirectory, $"yaca_{tag}_3.6.x.ts3_plugin");
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
                progress?.Report(new YacaUpdaterProgress(version, bytesReceived, totalBytes, "Download", false, false, null));
            }

            await output.FlushAsync(cancellationToken);
            output.Close();
            File.Move(partialPath, archivePath, true);

            await ProcessDownloadedArchiveAsync(version, archivePath, progress, cancellationToken);
            FinalizeDownloadedArchive(archivePath);
        }
        catch (OperationCanceledException)
        {
            TryDelete(partialPath);
            TryDelete(archivePath);
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException)
        {
            TryDelete(partialPath);
            TryDelete(archivePath);
            progress?.Report(new YacaUpdaterProgress(version, 0, null, "Fehlgeschlagen", true, false, ex.Message));
        }
    }

    private void FinalizeDownloadedArchive(string archivePath)
    {
        if (_service.Settings.KeepYacaPluginDownloads)
        {
            Directory.CreateDirectory(DownloadDirectory);
            File.Move(archivePath, Path.Combine(DownloadDirectory, Path.GetFileName(archivePath)), true);
            return;
        }

        TryDelete(archivePath);
    }

    private async Task ProcessExistingDownloadsAsync(
        IProgress<YacaUpdaterProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(DownloadDirectory))
            return;

        var archives = Directory
            .EnumerateFiles(DownloadDirectory, "yaca_*.ts3_plugin", SearchOption.TopDirectoryOnly)
            .Select(path => (Path: path, Version: ExtractVersion(Path.GetFileName(path))))
            .Where(item => item.Version is not null)
            .OrderByDescending(item => ParseVersion(item.Version!))
            .ToList();

        foreach (var archive in archives)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var version = archive.Version!;
            var tag = version.Replace(".", "", StringComparison.Ordinal);
            var target = Path.Combine(_service.Paths.PluginDirectory, $"yaca_{tag}_win64.dll");

            if (IsValidInstalledVersion(target, version))
            {
                if (!_service.Settings.KeepYacaPluginDownloads)
                    TryDelete(archive.Path);
                continue;
            }

            try
            {
                await ProcessDownloadedArchiveAsync(version, archive.Path, progress, cancellationToken);
                if (!_service.Settings.KeepYacaPluginDownloads)
                    TryDelete(archive.Path);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException)
            {
                progress?.Report(new YacaUpdaterProgress(version, 0, null, "Fehlgeschlagen", true, false, ex.Message));
            }
        }
    }

    private Task ProcessDownloadedArchiveAsync(
        string version,
        string archivePath,
        IProgress<YacaUpdaterProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("YACA-Download nicht gefunden.", archivePath);

        var tag = version.Replace(".", "", StringComparison.Ordinal);
        var target = Path.Combine(_service.Paths.PluginDirectory, $"yaca_{tag}_win64.dll");
        var extractionDirectory = Path.Combine(TempDirectory, "validate_" + Guid.NewGuid().ToString("N"));
        var tempDll = Path.Combine(extractionDirectory, "yaca_win64.dll");
        var tempTarget = Path.Combine(_service.Paths.PluginDirectory, $".yaca_install_{Guid.NewGuid():N}.tmp");

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

            Report(progress, version, _service.Settings.KeepYacaPluginDownloads ? "Download behalten" : "Download löschen", false, false, null);
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

    private static void ExtractYacaDll(string archivePath, string destinationPath)
    {
        using var zip = ZipFile.OpenRead(archivePath);
        var entry = zip.Entries.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(item.Name) && ArchiveDllRegex.IsMatch(Path.GetFileName(item.FullName)));

        if (entry is null)
            throw new InvalidDataException("Keine YACA x64-DLL im TS3-Plugin-Archiv gefunden.");

        entry.ExtractToFile(destinationPath, true);
    }

    private static void EnsureExtractedDllExists(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length == 0)
            throw new InvalidDataException("Die extrahierte DLL ist leer oder wurde nicht erstellt.");
    }

    private static void EnsureValidYacaVersion(ValidationResult validation, string expectedVersion)
    {
        if (!validation.IsValid || validation.Version is null || string.IsNullOrWhiteSpace(validation.Sha256))
            throw new InvalidDataException($"Die extrahierte DLL ist ungültig: {validation.Message}");

        if (!Version.TryParse(expectedVersion, out var expected) || validation.Version != expected)
            throw new InvalidDataException($"Versionsprüfung fehlgeschlagen: erwartet {expectedVersion}, erkannt {validation.Version}.");
    }

    private static void ValidateStagedDll(string path, ValidationResult sourceValidation, string expectedVersion)
    {
        var stagedValidation = YacaValidator.Validate(path);
        if (!stagedValidation.IsValid
            || !string.Equals(stagedValidation.Sha256, sourceValidation.Sha256, StringComparison.OrdinalIgnoreCase)
            || stagedValidation.Version is null
            || !Version.TryParse(expectedVersion, out var expected)
            || stagedValidation.Version != expected)
        {
            throw new InvalidDataException("Die temporär bereitgestellte DLL konnte nicht verifiziert werden.");
        }
    }

    private static void ValidateInstalledDll(string path, ValidationResult sourceValidation, string expectedVersion)
    {
        var installedValidation = YacaValidator.Validate(path);
        if (!installedValidation.IsValid
            || !string.Equals(installedValidation.Sha256, sourceValidation.Sha256, StringComparison.OrdinalIgnoreCase)
            || installedValidation.Version is null
            || !Version.TryParse(expectedVersion, out var expected)
            || installedValidation.Version != expected)
        {
            throw new InvalidDataException("Die installierte DLL konnte nach dem Verschieben nicht verifiziert werden.");
        }
    }

    private HashSet<string> GetValidatedLocalVersions()
    {
        if (!Directory.Exists(_service.Paths.PluginDirectory))
            return [];

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(_service.Paths.PluginDirectory, "yaca_*_win64.dll", SearchOption.TopDirectoryOnly))
        {
            var validation = YacaValidator.Validate(file);
            if (!validation.IsValid || validation.Version is null)
                continue;

            result.Add(validation.Version.ToString().Replace(".", "", StringComparison.Ordinal));
        }

        return result;
    }

    private static bool IsValidInstalledVersion(string path, string expectedVersion)
    {
        if (!File.Exists(path))
            return false;

        var validation = YacaValidator.Validate(path);
        return validation.IsValid
            && validation.Version is not null
            && Version.TryParse(expectedVersion, out var expected)
            && validation.Version == expected
            && !string.IsNullOrWhiteSpace(validation.Sha256);
    }

    private static async Task LoadVersionsAsync(
        HttpClient http,
        string url,
        IDictionary<long, string> versions,
        CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        ExtractVersions(document.RootElement, versions);
    }

    /// <summary>
    /// Liest ausschließlich explizit als Version bezeichnete JSON-Felder.
    /// Beliebige Stringwerte der API dürfen niemals als YACA-Version interpretiert werden.
    /// </summary>
    private static void ExtractVersions(JsonElement element, IDictionary<long, string> versions)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (IsVersionProperty(property.Name))
                        AddVersion(property.Value, versions);
                    else
                        ExtractVersions(property.Value, versions);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    ExtractVersions(item, versions);
                break;
        }
    }

    private static bool IsVersionProperty(string propertyName) =>
        string.Equals(propertyName, "version", StringComparison.OrdinalIgnoreCase)
        || string.Equals(propertyName, "yacaVersion", StringComparison.OrdinalIgnoreCase)
        || string.Equals(propertyName, "yaca_version", StringComparison.OrdinalIgnoreCase);

    private static void AddVersion(JsonElement value, IDictionary<long, string> versions)
    {
        if (value.ValueKind != JsonValueKind.String)
            return;

        var trimmed = value.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)
            || !VersionStringRegex.IsMatch(trimmed)
            || !Version.TryParse(trimmed, out _))
            return;

        versions[ParseVersion(trimmed)] = trimmed;
    }

    private static string? ExtractVersion(string fileName)
    {
        var archiveMatch = VersionArchiveRegex.Match(fileName);
        if (archiveMatch.Success)
        {
            var value = archiveMatch.Groups[1].Value;
            if (VersionStringRegex.IsMatch(value))
                return value;
            return DecodeCompactVersion(value);
        }

        var dllMatch = VersionDigitsRegex.Match(fileName);
        return dllMatch.Success ? DecodeCompactVersion(dllMatch.Groups[1].Value) : null;
    }
