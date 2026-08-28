using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher.Core;

public sealed class YacaScanner
{
    private readonly Logger _logger;

    public YacaScanner(Logger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<YacaPluginInfo> Scan(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return [];

        var result = new List<YacaPluginInfo>();
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                .Where(path => string.Equals(Path.GetExtension(path), ".dll", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Warn($"Plugin-Ordner konnte nicht gelesen werden: {ex.Message}");
            return [];
        }

        var discoveredDllCount = 0;
        foreach (var path in files)
        {
            discoveredDllCount++;
            _logger.Debug($"YACA-Scan prüft DLL: {Path.GetFileName(path)}");
            try
            {
                var validation = YacaValidator.Validate(path);
                var fileName = Path.GetFileName(path);
                if (validation.IsValid && validation.Version is not null && !string.IsNullOrWhiteSpace(validation.Sha256))
                {
                    result.Add(new YacaPluginInfo(
                        path,
                        fileName,
                        validation.Version,
                        validation.Build,
                        validation.FileSize,
                        validation.Sha256,
                        true,
                        validation.Message));
                    _logger.Info($"YACA erkannt: {fileName} -> {result[^1].DisplayName}");
                }
                else
                {
                    _logger.Debug($"YACA-Scan verwirft DLL: {fileName} -> {validation.Message}");
                    _logger.Warn($"DLL ignoriert: {fileName} -> {validation.Message}");
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
            {
                _logger.Warn($"DLL konnte nicht geprüft werden: {Path.GetFileName(path)} -> {ex.Message}");
            }
        }

        _logger.Info($"YACA-Scan: {discoveredDllCount} DLL-Datei(en) gefunden, {result.Count} gültig in {directory}");

        return result
            .OrderBy(plugin => plugin.Version)
            .ThenBy(plugin => plugin.Build ?? long.MinValue)
            .ThenBy(plugin => plugin.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
