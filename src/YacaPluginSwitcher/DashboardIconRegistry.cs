using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SharpVectors.Converters;

namespace YacaPluginSwitcher;

/// <summary>
/// Single source of truth for all dashboard/navigation icon assets.
/// The registry owns asset names and SVG resource locations. The central
/// renderer remains responsible for size, placement and context.
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
    internal const string IconAssetInstalled = "icon_asset_installed";
    internal const string IconAssetTeamSpeak = "icon_asset_teamspeak";
    internal const string IconAssetTeamSpeakActive = "icon_asset_teamspeak_active";
    internal const string IconAssetTeamSpeakInactive = "icon_asset_teamspeak_inactive";
    internal const string IconAssetTeamSpeakStatus = "icon_asset_teamspeak_status";
    internal const string IconAssetSort = "icon_asset_sort";

    private static readonly Dictionary<string, string> AssetPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        [IconAssetDashboard] = "/Assets/dashboard-home-icon.svg",
        [IconAssetRefresh] = "/Assets/refresh-update-icon.svg",
        [IconAssetSync] = "/Assets/sync-icon.svg",
        [IconAssetBackup] = "/Assets/data-update-icon.svg",
        [IconAssetBackups] = "/Assets/data-update-icon.svg",
        [IconAssetUpdater] = "/Assets/sync-icon.svg",
        [IconAssetInfo] = "/Assets/info-notepad-icon.svg",
        [IconAssetExit] = "/Assets/power-off-icon.svg",
        [IconAssetInstalled] = "/Assets/checked-shield-icon.svg",
        [IconAssetTeamSpeak] = "/Assets/ts_stacked_light.svg",
        [IconAssetTeamSpeakActive] = "/Assets/ts_stacked_light.svg",
        [IconAssetTeamSpeakInactive] = "/Assets/ts_stacked_light.svg",
        [IconAssetTeamSpeakStatus] = "/Assets/ts_stacked_light.svg",
        [IconAssetSort] = "/Assets/sort-toggle-icon.svg"
    };

    internal static bool TryGetAssetPath(string assetKey, out string path) => AssetPaths.TryGetValue(assetKey, out path!);

    internal static Image CreateIcon(string assetKey, Brush fill, double width, double height)
    {
        if (!TryGetAssetPath(assetKey, out var path))
            throw new InvalidOperationException($"Unknown dashboard icon asset '{assetKey}'.");

        return new SvgIcon
        {
            UriSource = new Uri(path, UriKind.RelativeOrAbsolute),
            AppName = "YacaPluginSwitcher",
            Fill = fill,
            Width = width,
            Height = height,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
            Tag = assetKey
        };
    }

    internal static void SetFill(Image icon, Brush fill)
    {
        if (icon is SvgIcon svgIcon)
            svgIcon.Fill = fill;
    }
}
