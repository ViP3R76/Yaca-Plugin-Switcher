using System.Globalization;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher.Core;

public sealed record YacaUpdaterProgress(string Version, long BytesReceived, long? TotalBytes, string Status, bool Completed, bool Success, string? Error);

public sealed class YacaUpdaterService
{
    private const string CdnBase = "https://cdn.yaca.systems/yaca_{0}_3.6.x.ts3_plugin";
    private const string DllInZip = "plugins/yaca_win64.dll";
    private static readonly CompositeFormat CdnFormat = CompositeFormat.Parse(CdnBase);
    private static readonly Version MinExclusiveVersion = new(1, 7, 5);
    private readonly YacaService _service;

    public YacaUpdaterService(YacaService service) => _service = service;
    public static string DownloadDirectory => Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory, "plugins_download");

    public Task<IReadOnlyList<(string Version, string FileName, long Size)>> GetAvailableDownloadsAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(DownloadDirectory);
        IReadOnlyList<(string Version, string FileName, long Size)> result = Directory.EnumerateFiles(DownloadDirectory, "*.ts3_plugin", SearchOption.TopDirectoryOnly)
            .Select(path => (Version: ExtractVersion(Path.GetFileName(path)) ?? "—", FileName: Path.GetFileName(path), Size: new FileInfo(path).Length))
            .OrderByDescending(x => ParseVersion(x.Version)).ToList();
        return Task.FromResult(result);
    }

    public async Task<IReadOnlyList<string>> GetMissingVersionsAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(DownloadDirectory);
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var versions = new SortedDictionary<long, string>();
        await LoadVersionsAsync(http, "https://yaca.systems/api/changelog/getDownloads", versions, cancellationToken);
        await LoadVersionsAsync(http, "https://yaca.systems/api/changelog/get?locale=en", versions, cancellationToken);
        var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(_service.Paths.PluginDirectory, "yaca_*_win64.dll"))
        {
            var match = Regex.Match(Path.GetFileName(file), @"^yaca_(\d+)_win64\.dll$", RegexOptions.IgnoreCase);
            if (match.Success) local.Add(match.Groups[1].Value);
        }
        foreach (var file in Directory.EnumerateFiles(DownloadDirectory, "yaca_*.ts3_plugin"))
        {
            var version = ExtractVersion(Path.GetFileName(file));
            if (!string.IsNullOrWhiteSpace(version)) local.Add(version.Replace(".", ""));
        }
        return versions.Where(kv => Version.TryParse(kv.Value, out var version) && version >= MinExclusiveVersion && !local.Contains(kv.Value.Replace(".", ""))).Select(kv => kv.Value).OrderByDescending(ParseVersion).ToList();
    }

    public async Task DownloadMissingAsync(IProgress<YacaUpdaterProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(DownloadDirectory);
        var missing = await GetMissingVersionsAsync(cancellationToken);
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        foreach (var version in missing)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tag = version.Replace(".", "");
            var archivePath = Path.Combine(DownloadDirectory, $"yaca_{tag}_3.6.x.ts3_plugin");
            try
            {
                progress?.Report(new(version, 0, null, "Download wird vorbereitet", false, false, null));
                using var response = await http.GetAsync(string.Format(CdnFormat, CultureInfo.InvariantCulture, version), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength;
                await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var output = File.Create(archivePath);
                var buffer = new byte[81920]; long received = 0; int read;
                while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken); received += read;
                    progress?.Report(new(version, received, total, "Download läuft", false, false, null));
                }
                await ValidateAndInstallAsync(version, archivePath, progress, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or UnauthorizedAccessException)
            {
                progress?.Report(new(version, 0, null, "Download fehlgeschlagen", true, false, ex.Message));
            }
        }
    }

    private async Task ValidateAndInstallAsync(string version, string archivePath, IProgress<YacaUpdaterProgress>? progress, CancellationToken cancellationToken)
    {
        var archiveSize = new FileInfo(archivePath).Length;
        progress?.Report(new(version, archiveSize, archiveSize, "Archiv wird geprüft", false, false, null));
        var tag = version.Replace(".", "");
        var target = Path.Combine(_service.Paths.PluginDirectory, $"yaca_{tag}_win64.dll");
        var extraction = Path.Combine(Path.GetTempPath(), "yaca_validate_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(extraction);
        try
        {
            using var zip = ZipFile.OpenRead(archivePath);
            var entry = zip.GetEntry(DllInZip) ?? throw new InvalidDataException($"{DllInZip} fehlt im Archiv.");
            var tempDll = Path.Combine(extraction, $"yaca_{tag}_win64.dll");
            entry.ExtractToFile(tempDll, true);
            if (new FileInfo(tempDll).Length == 0) throw new InvalidDataException("Die extrahierte DLL ist leer.");
            progress?.Report(new(version, archiveSize, archiveSize, "DLL wird validiert", false, false, null));
            Directory.CreateDirectory(_service.Paths.PluginDirectory);
            File.Move(tempDll, target, true);
            if (!_service.ScanPlugins().Any(p => string.Equals(Path.GetFullPath(p.FilePath), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("Die DLL wurde nach der Installation nicht als gültiges YACA Plugin erkannt.");
            progress?.Report(new(version, archiveSize, archiveSize, "Erfolgreich hinzugefügt", true, true, null));
        }
        finally { try { Directory.Delete(extraction, true); } catch { } }
        await Task.CompletedTask;
    }

    private static async Task LoadVersionsAsync(HttpClient http, string url, SortedDictionary<long, string> versions, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await http.GetStreamAsync(url, cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (!el.TryGetProperty("version", out var property)) continue;
                var version = property.GetString();
                if (string.IsNullOrWhiteSpace(version) || !Regex.IsMatch(version, @"^\d+\.\d+\.\d+$")) continue;
                if (long.TryParse(version.Replace(".", ""), out var number)) versions[number] = version;
            }
        }
        catch { }
    }

    private static Version ParseVersion(string value) => Version.TryParse(value, out var version) ? version : new Version(0, 0, 0);
    private static string? ExtractVersion(string fileName)
    {
        var match = Regex.Match(fileName, @"yaca_(\d+)(?:_3\.6\.x)?\.ts3_plugin", RegexOptions.IgnoreCase);
        if (!match.Success) return null;
        var digits = match.Groups[1].Value;
        return digits.Length == 3 ? $"{digits[0]}.{digits[1]}.{digits[2]}" : null;
    }
}
