using System.Globalization;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher.Core;

public sealed record YacaUpdaterProgress(string Version, long BytesReceived, long? TotalBytes, string Status, bool Completed, bool Success, string? Error);

public sealed class YacaUpdaterService
{
    private const string CdnBase = "https://cdn.yaca.systems/yaca_{0}_3.6.x.ts3_plugin";
    private static readonly CompositeFormat CdnFormat = CompositeFormat.Parse(CdnBase);
    private static readonly Version MinExclusiveVersion = new(1, 7, 5);
    private static readonly Regex ArchiveDllRegex = new(@"^yaca(?:_\d+)?_win64\.dll$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly YacaService _service;

    public YacaUpdaterService(YacaService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public string DownloadDirectory => Path.Combine(_service.Paths.BaseDirectory, "plugins_download");
    public string TempDirectory => _service.Paths.TempDirectory;

    public Task<IReadOnlyList<(string Version, string FileName, long Size)>> GetAvailableDownloadsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(DownloadDirectory);
        IReadOnlyList<(string Version, string FileName, long Size)> result = Directory.EnumerateFiles(DownloadDirectory, "*.ts3_plugin", SearchOption.TopDirectoryOnly)
            .Select(path => (Version: ExtractVersion(Path.GetFileName(path)) ?? "—", FileName: Path.GetFileName(path), Size: new FileInfo(path).Length))
            .OrderByDescending(x => ParseVersion(x.Version))
            .ToList();
        return Task.FromResult(result);
    }

    public async Task<IReadOnlyList<string>> GetMissingVersionsAsync(CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var versions = new SortedDictionary<long, string>();
        await LoadVersionsAsync(http, "https://yaca.systems/api/changelog/getDownloads", versions, cancellationToken);
        await LoadVersionsAsync(http, "https://yaca.systems/api/changelog/get?locale=en", versions, cancellationToken);

        // Nur eine bereits validierte DLL im Versionsbestand zählt als vorhanden.
        // Ein .ts3_plugin-Archiv allein zählt niemals als vorhandene Version.
        var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(_service.Paths.PluginDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(_service.Paths.PluginDirectory, "yaca_*_win64.dll", SearchOption.TopDirectoryOnly))
            {
                var match = Regex.Match(Path.GetFileName(file), @"^yaca_(\d+)_win64\.dll$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (!match.Success)
                    continue;

                var digits = match.Groups[1].Value;
                if (digits.Length != 3 || !Version.TryParse($"{digits[0]}.{digits[1]}.{digits[2]}", out var expected))
                    continue;

                var validation = YacaValidator.Validate(file);
                if (validation.IsValid && validation.Version is not null && validation.Version == expected)
                    local.Add(digits);
            }
        }

        return versions
            .Where(kv => Version.TryParse(kv.Value, out var version)
                         && version > MinExclusiveVersion
                         && !local.Contains(kv.Value.Replace(".", "", StringComparison.Ordinal)))
            .Select(kv => kv.Value)
            .OrderByDescending(ParseVersion)
            .ToList();
    }

    public async Task DownloadMissingAsync(IProgress<YacaUpdaterProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(DownloadDirectory);
        Directory.CreateDirectory(TempDirectory);
        CleanTemporaryUpdaterFiles();
        await ProcessExistingDownloadsAsync(progress, cancellationToken);

        var missing = await GetMissingVersionsAsync(cancellationToken);
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

        foreach (var version in missing)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tag = version.Replace(".", "", StringComparison.Ordinal);
            var archivePath = Path.Combine(TempDirectory, $"yaca_{tag}_3.6.x.ts3_plugin");
            var partialPath = archivePath + ".part";

            try
            {
                TryDelete(archivePath);
                TryDelete(partialPath);
                Report(progress, version, "Download", false, false, null);
                using var response = await http.GetAsync(string.Format(CultureInfo.InvariantCulture, CdnFormat, version), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength;
                await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var output = File.Create(partialPath);
                var buffer = new byte[81920];
                long received = 0;
                int read;
                while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    received += read;
                    progress?.Report(new(version, received, total, "Download", false, false, null));
                }
                await output.FlushAsync(cancellationToken);
                output.Close();
                File.Move(partialPath, archivePath, true);
                await ProcessDownloadedArchiveAsync(version, archivePath, progress, cancellationToken);

                if (_service.Settings.KeepYacaPluginDownloads)
                {
                    Directory.CreateDirectory(DownloadDirectory);
                    File.Move(archivePath, Path.Combine(DownloadDirectory, Path.GetFileName(archivePath)), true);
                }
                else
                {
                    TryDelete(archivePath);
                }
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
                progress?.Report(new(version, 0, null, "Fehlgeschlagen", true, false, ex.Message));
            }
        }
    }

    private async Task ProcessExistingDownloadsAsync(IProgress<YacaUpdaterProgress>? progress, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(DownloadDirectory))
            return;

        var archives = Directory.EnumerateFiles(DownloadDirectory, "yaca_*.ts3_plugin", SearchOption.TopDirectoryOnly)
            .Select(path => (Path: path, Version: ExtractVersion(Path.GetFileName(path))))
            .Where(x => !string.IsNullOrWhiteSpace(x.Version))
            .OrderByDescending(x => ParseVersion(x.Version!))
            .ToList();

        foreach (var archive in archives)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var version = archive.Version!;
            var tag = version.Replace(".", "", StringComparison.Ordinal);
            var target = Path.Combine(_service.Paths.PluginDirectory, $"yaca_{tag}_win64.dll");

            if (File.Exists(target))
            {
                var validation = YacaValidator.Validate(target);
                if (validation.IsValid && validation.Version is not null && validation.Version.Equals(ParseVersion(version)))
                {
                    if (!_service.Settings.KeepYacaPluginDownloads)
                        TryDelete(archive.Path);
                    continue;
                }
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
                progress?.Report(new(version, 0, null, "Fehlgeschlagen", true, false, ex.Message));
            }
        }
    }

    private async Task ProcessDownloadedArchiveAsync(string version, string archivePath, IProgress<YacaUpdaterProgress>? progress, CancellationToken cancellationToken)
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
            using (var zip = ZipFile.OpenRead(archivePath))
            {
                var entry = zip.Entries.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Name) && ArchiveDllRegex.IsMatch(Path.GetFileName(e.FullName)));
                if (entry is null)
                    throw new InvalidDataException("Keine YACA x64-DLL im TS3-Plugin-Archiv gefunden.");
                entry.ExtractToFile(tempDll, true);
            }

            Report(progress, version, "Prüfung", false, false, null);
            if (!File.Exists(tempDll) || new FileInfo(tempDll).Length == 0)
                throw new InvalidDataException("Die extrahierte DLL ist leer oder wurde nicht erstellt.");

            Report(progress, version, "Validierung", false, false, null);
            var validation = YacaValidator.Validate(tempDll);
            if (!validation.IsValid || validation.Version is null || string.IsNullOrWhiteSpace(validation.Sha256))
                throw new InvalidDataException($"Die extrahierte DLL ist ungültig: {validation.Message}");
            if (!Version.TryParse(version, out var expectedVersion) || validation.Version != expectedVersion)
                throw new InvalidDataException($"Versionsprüfung fehlgeschlagen: erwartet {version}, erkannt {validation.Version}.");

            Directory.CreateDirectory(_service.Paths.PluginDirectory);
            Report(progress, version, "Verschieben", false, false, null);
            File.Copy(tempDll, tempTarget, false);
            var stagedValidation = YacaValidator.Validate(tempTarget);
            if (!stagedValidation.IsValid || !string.Equals(stagedValidation.Sha256, validation.Sha256, StringComparison.OrdinalIgnoreCase) || stagedValidation.Version != expectedVersion)
                throw new InvalidDataException("Die temporär bereitgestellte DLL konnte nicht verifiziert werden.");

            File.Move(tempTarget, target, true);
            var installedValidation = YacaValidator.Validate(target);
            if (!installedValidation.IsValid || !string.Equals(installedValidation.Sha256, validation.Sha256, StringComparison.OrdinalIgnoreCase) || installedValidation.Version != expectedVersion)
                throw new InvalidDataException("Die verschobene DLL konnte nicht erfolgreich verifiziert werden.");

            Report(progress, version, _service.Settings.KeepYacaPluginDownloads ? "Download behalten" : "Download löschen", false, false, null);
            Report(progress, version, "Abgeschlossen", true, true, null);
        }
        finally
        {
            TryDelete(tempDll);
            TryDelete(tempTarget);
            try { if (Directory.Exists(extractionDirectory)) Directory.Delete(extractionDirectory, true); } catch { }
        }

        await Task.CompletedTask;
    }

    private void CleanTemporaryUpdaterFiles()
    {
        if (!Directory.Exists(TempDirectory))
            return;
        foreach (var file in Directory.EnumerateFiles(TempDirectory, "yaca_*.ts3_plugin*", SearchOption.TopDirectoryOnly))
            TryDelete(file);
        foreach (var directory in Directory.EnumerateDirectories(TempDirectory, "validate_*", SearchOption.TopDirectoryOnly))
        {
            try { Directory.Delete(directory, true); } catch { }
        }
    }

    private static void Report(IProgress<YacaUpdaterProgress>? progress, string version, string status, bool completed, bool success, string? error)
        => progress?.Report(new(version, 0, null, status, completed, success, error));

    private static async Task LoadVersionsAsync(HttpClient http, string url, SortedDictionary<long, string> versions, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await http.GetStreamAsync(url, cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return;
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (!el.TryGetProperty("version", out var property)) continue;
                var version = property.GetString();
                if (string.IsNullOrWhiteSpace(version) || !Regex.IsMatch(version, @"^\d+\.\d+\.\d+$")) continue;
                if (long.TryParse(version.Replace(".", "", StringComparison.Ordinal), out var number)) versions[number] = version;
            }
        }
        catch (HttpRequestException) { }
        catch (JsonException) { }
    }

    private static Version ParseVersion(string value) => Version.TryParse(value, out var version) ? version : new Version(0, 0, 0);

    private static string? ExtractVersion(string fileName)
    {
        var match = Regex.Match(fileName, @"yaca_(\d+)(?:_3\.6\.x)?\.ts3_plugin", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success) return null;
        var digits = match.Groups[1].Value;
        return digits.Length == 3 ? $"{digits[0]}.{digits[1]}.{digits[2]}" : null;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
