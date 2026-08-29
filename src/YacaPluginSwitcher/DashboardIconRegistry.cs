using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace YacaPluginSwitcher;

/// <summary>
/// Central registry for dashboard/navigation icon assets and rendering metadata.
/// The dashboard renderer remains the single renderer; this class only owns icon definitions.
/// </summary>
internal static class DashboardIconRegistry
{
    private static readonly IReadOnlyDictionary<string, string> AssetPaths =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["refresh"] = "/YacaPluginSwitcher;component/Assets/refresh-update-icon.svg",
            ["updater"] = "/YacaPluginSwitcher;component/Assets/sync-icon.svg",
            ["info"] = "/YacaPluginSwitcher;component/Assets/info-notepad-icon.svg",
            ["backups"] = "/YacaPluginSwitcher;component/Assets/backup-database-icon.svg",
            ["backup"] = "/YacaPluginSwitcher;component/Assets/data-update-icon.svg",
            ["switch"] = "/YacaPluginSwitcher;component/Assets/sync-icon.svg",
            ["exit"] = "/YacaPluginSwitcher;component/Assets/power-off-icon.svg"
        };

    public static bool TryGetAssetPath(string key, out string path) => AssetPaths.TryGetValue(key, out path!);

    public static ImageSource? Load(string key)
    {
        if (!TryGetAssetPath(key, out var path)) return null;
        return new System.Windows.Media.Imaging.BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
    }
}
