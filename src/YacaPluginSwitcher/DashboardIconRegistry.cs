using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace YacaPluginSwitcher;

/// <summary>
/// Single source of truth for all dashboard/navigation icon assets.
/// Asset names are intentionally explicit and are reused wherever the same
/// physical icon is required. Rendering (size, colour and placement) remains
/// the responsibility of the central DashboardRenderer/MainWindow.
/// </summary>
internal static class DashboardIconRegistry
{
    internal const string IconAssetDashboard = "icon_asset_dashboard";
    internal const string IconAssetRefresh = "icon_asset_refresh";
    internal const string IconAssetSync = "icon_asset_sync";
    internal const string IconAssetBackup = "icon_asset_backup";
    internal const string IconAssetBackups = "icon_asset_backups";
    internal const string IconAssetUpdater = "icon_asset_updater";
    internal const string IconAssetInfo = "icon_asset_info";
    internal const string IconAssetExit = "icon_asset_exit";
    internal const string IconAssetTeamSpeak = "icon_asset_teamspeak";
    internal const string IconAssetTeamSpeakActive = "icon_asset_teamspeak_active";
    internal const string IconAssetTeamSpeakInactive = "icon_asset_teamspeak_inactive";
    internal const string IconAssetSort = "icon_asset_sort";

    private static readonly Dictionary<string, string> AssetPaths =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [IconAssetDashboard] = "/YacaPluginSwitcher;component/Assets/dashboard-home-icon.svg",
            [IconAssetRefresh] = "/YacaPluginSwitcher;component/Assets/refresh-update-icon.svg",
            [IconAssetSync] = "/YacaPluginSwitcher;component/Assets/sync-icon.svg",
            [IconAssetBackup] = "/YacaPluginSwitcher;component/Assets/data-update-icon.svg",
            [IconAssetBackups] = "/YacaPluginSwitcher;component/Assets/data-update-icon.svg",
            [IconAssetUpdater] = "/YacaPluginSwitcher;component/Assets/sync-icon.svg",
            [IconAssetInfo] = "/YacaPluginSwitcher;component/Assets/info-notepad-icon.svg",
            [IconAssetExit] = "/YacaPluginSwitcher;component/Assets/power-off-icon.svg"
        };

    public static bool TryGetAssetPath(string assetKey, out string path) =>
        AssetPaths.TryGetValue(assetKey, out path!);

    public static ImageSource? Load(string assetKey)
    {
        if (!TryGetAssetPath(assetKey, out var path))
            return null;

        return new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
    }
}
