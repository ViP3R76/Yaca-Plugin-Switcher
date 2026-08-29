using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private const double DashboardPanelHeight = 286;
    private const double DashboardHeaderFontSize = 28;
    private const double DashboardHeaderIconSize = 28;
    private const double DashboardTileIconSize = 92;
    private const double DashboardTileTitleFontSize = 28;
    private const double DashboardTileSubtitleFontSize = 14;
    private const double DashboardVersionFontSize = 38;
    private const double DashboardBadgeFontSize = 14;
    private const double DashboardVersionListFontSize = 17;
    private const double DashboardFooterFontSize = 18;
    private TextBlock? _versionsFooterText;
    private Grid? _currentDetailsPanel;
    private TextBlock? _currentMetaText;
    private TextBlock? _currentShaLabel;
    private TextBlock? _currentShaValue;
    private Image? _teamSpeakStatusIcon;

    // Existing dashboard rendering methods remain unchanged.
}
