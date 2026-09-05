using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

public partial class InfoView : UserControl
{
    private readonly bool _german;

    public InfoView(string? language)
    {
        _german = Localization.Normalize(language) == Localization.German;
        InitializeComponent();
        SizeChanged += InfoView_SizeChanged;
        Build();
        UpdateCommunityLayout();
    }

    private void Build()
    {
        PageSubtitle.Text = _german ? "Wichtige Informationen, offizielle Seiten, Community-Verbindungen und Support." : "Important information, official resources, community connections and support.";
        YacaDescription.Text = _german ? "YACA Systems stellt das Plugin bereit, das dieser Switcher verwaltet. Hier findest du die wichtigsten offiziellen Ressourcen für Homepage, Downloads, Lizenzen, FAQ und Community." : "YACA Systems provides the plugin managed by this switcher. Find the key official resources for the homepage, downloads, licenses, FAQ and community here.";
        TsDescription.Text = _german ? "Für dieses Tool ist ausdrücklich der TeamSpeak 3 Client erforderlich – nicht TeamSpeak 6. Die folgenden Links führen zu den wichtigsten offiziellen TeamSpeak-Ressourcen." : "This tool specifically requires the TeamSpeak 3 client, not TeamSpeak 6. The following links lead to the key official TeamSpeak resources.";
        CommunityDescription.Text = _german ? "Die SnakeNest Community verbindet Community, Streams und die Projekte rund um YACA Plugin Switcher." : "The SnakeNest Community brings together the community, streams and projects around YACA Plugin Switcher.";
        DiscordLinkText.Text = "Discord Community";
        TwitchLinkText.Text = "Twitch Stream";
        GitHubLinkText.Text = _german ? "Projekt auf GitHub" : "Project on GitHub";
        TwitchIconHost.Content = DashboardIconRegistry.CreateNaturalIcon(DashboardIconRegistry.IconAssetTwitch, 28, 28);
        YacaLinks.ItemsSource = CreateLinkItems(_german
            ? new[] { new LinkItem("YACA Homepage", "https://yaca.systems/"), new LinkItem("YACA Downloads", "https://yaca.systems/download"), new LinkItem("YACA Lizenzen", "https://yaca.systems/licenses"), new LinkItem("YACA FAQ", "https://yaca.systems/faq"), new LinkItem("YACA Discord", "https://discord.yaca.systems/") }
            : new[] { new LinkItem("YACA Homepage", "https://yaca.systems/"), new LinkItem("YACA Downloads", "https://yaca.systems/download"), new LinkItem("YACA Licenses", "https://yaca.systems/licenses"), new LinkItem("YACA FAQ", "https://yaca.systems/faq"), new LinkItem("YACA Discord", "https://discord.yaca.systems/") });
        TsLinks.ItemsSource = CreateLinkItems(_german
            ? new[] { new LinkItem("TeamSpeak Homepage", "https://www.teamspeak.com/de/"), new LinkItem("TeamSpeak 3 Downloads", "https://www.teamspeak.com/de/downloads/?product=ts3"), new LinkItem("TeamSpeak Support", "https://support.teamspeak.com/hc/de") }
            : new[] { new LinkItem("TeamSpeak Homepage", "https://www.teamspeak.com/en/"), new LinkItem("TeamSpeak 3 Downloads", "https://www.teamspeak.com/en/downloads/?product=ts3"), new LinkItem("TeamSpeak Support", "https://support.teamspeak.com/hc/en-us") });
        LegalHeader.Text = "DISCLAIMER";
        LegalText.Text = _german ? "Diese Anwendung wird unabhängig entwickelt und steht in keiner geschäftlichen, rechtlichen oder technischen Verbindung zu YACA Systems, TeamSpeak Systems GmbH oder anderen in den verlinkten Ressourcen genannten Drittanbietern. YACA und TeamSpeak sind eigenständige Produkte und Marken ihrer jeweiligen Rechteinhaber. Die Links in dieser Anwendung dienen ausschließlich der Navigation zu externen Ressourcen; aus ihrer Aufnahme ergibt sich keine Empfehlung, Partnerschaft oder Unterstützung durch die genannten Anbieter. Die Verwendung dieses Switchers erfolgt auf eigene Verantwortung. Vor Installations-, Backup-, Restore- oder Wechselvorgängen sind Zielpfad und ausgewählte Version zu prüfen. Stelle insbesondere sicher, dass TeamSpeak 3 nicht auf die betroffene YACA-Datei zugreift. Backups ersetzen keine unabhängige Datensicherung. Für Inhalte, Verfügbarkeit, Preise, Lizenzen, Downloads oder Änderungen externer Webseiten und Dienste wird keine Gewähr übernommen. Maßgeblich sind stets die aktuellen Angaben der jeweiligen Anbieter. Externe Links öffnen sich in deinem Standardbrowser." : "This application is developed independently and has no business, legal or technical affiliation with YACA Systems, TeamSpeak Systems GmbH, or other third parties referenced by the linked resources. YACA and TeamSpeak are independent products and trademarks of their respective rights holders. Links in this application are provided solely for navigation to external resources; their inclusion does not imply endorsement, partnership or support by the referenced providers. Use of this switcher is at your own responsibility. Before installation, backup, restore or switching operations, verify the intended target path and selected version. In particular, make sure TeamSpeak 3 is not accessing the affected YACA file. Backups are not a substitute for an independent data backup strategy. No warranty is provided for the content, availability, pricing, licensing, downloads or changes of external websites and services. The current information provided by the respective providers is authoritative. External links open in your default browser.";
    }

    private LinkItem[] CreateLinkItems(IEnumerable<LinkItem> links)
    {
        var accent = (SolidColorBrush)FindResource("AccentBrush");
        return links
            .Select((link, index) => link with { Background = GetLinkRowBackground(accent, index) })
            .ToArray();
    }

    private static Brush GetLinkRowBackground(SolidColorBrush accent, int index)
    {
        if (index % 2 == 0)
            return Brushes.Transparent;

        return new SolidColorBrush(Color.FromArgb(18, accent.Color.R, accent.Color.G, accent.Color.B));
    }

    private void InfoView_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateCommunityLayout();

    private void UpdateCommunityLayout()
    {
        if (CommunityLinksGrid is null) return;
        var availableWidth = CommunityLinksGrid.ActualWidth;
        var useTwoRows = availableWidth > 0 && availableWidth < 760;
        CommunityLinksGrid.Columns = useTwoRows ? 2 : 4;
        CommunityLinksGrid.Rows = useTwoRows ? 2 : 1;
    }

    private static void Open(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }

    private void Link_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string url }) Open(url);
    }

    private sealed record LinkItem(string Text, string Url, Brush? Background = null);
}
