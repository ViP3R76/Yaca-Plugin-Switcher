using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace YacaPluginSwitcher;

public partial class InfoView : UserControl
{
    private readonly bool _german;

    public InfoView(string? language)
    {
        _german = Localization.Normalize(language) == Localization.German;
        InitializeComponent();
        Build();
    }

    private void Build()
    {
        PageSubtitle.Text = _german
            ? "Wichtige Informationen, offizielle Seiten, Community-Verbindungen und Support."
            : "Important information, official resources, community connections and support.";

        YacaDescription.Text = _german
            ? "YACA Systems stellt das Plugin bereit, das dieser Switcher verwaltet. Hier findest du offizielle Informationen, Lizenzen, Nutzungsbedingungen, FAQ und den offiziellen Discord."
            : "YACA Systems provides the plugin managed by this switcher. Find official information, licenses, terms of service, FAQ and the official Discord here.";

        TsDescription.Text = _german
            ? "Für dieses Tool ist ausdrücklich der TeamSpeak 3 Client erforderlich – nicht TeamSpeak 6. Die folgenden Links führen direkt zu den offiziellen TeamSpeak-Ressourcen."
            : "This tool specifically requires the TeamSpeak 3 client, not TeamSpeak 6. The following links lead directly to official TeamSpeak resources.";

        CommunityDescription.Text = _german
            ? "Die SnakeNest Community verbindet Community, Streams und die Projekte rund um YACA Plugin Switcher."
            : "The SnakeNest Community brings together the community, streams and projects around YACA Plugin Switcher.";

        DiscordLinkText.Text = "Discord Community";
        TwitchLinkText.Text = "Twitch Stream";
        GitHubLinkText.Text = _german ? "Projekt auf GitHub" : "Project on GitHub";
        KofiLinkText.Text = "Ko-fi Support";

        YacaLinks.ItemsSource = _german
            ? new[]
            {
                ("YACA Homepage", "https://yaca.systems/"),
                ("YACA Lizenzen", "https://yaca.systems/licenses"),
                ("YACA Nutzungsbedingungen", "https://yaca.systems/tos"),
                ("YACA FAQ", "https://yaca.systems/faq"),
                ("YACA Discord", "https://discord.yaca.systems/")
            }
            : new[]
            {
                ("YACA Homepage", "https://yaca.systems/"),
                ("YACA Licenses", "https://yaca.systems/licenses"),
                ("YACA Terms of Service", "https://yaca.systems/tos"),
                ("YACA FAQ", "https://yaca.systems/faq"),
                ("YACA Discord", "https://discord.yaca.systems/")
            };

        TsLinks.ItemsSource = _german
            ? new[]
            {
                ("TeamSpeak Homepage", "https://www.teamspeak.com/"),
                ("TeamSpeak 3 Client", "https://teamspeak.com/de/downloads?product=ts3"),
                ("TeamSpeak Support", "https://support.teamspeak.com/")
            }
            : new[]
            {
                ("TeamSpeak Homepage", "https://www.teamspeak.com/"),
                ("TeamSpeak 3 Client", "https://teamspeak.com/en/downloads?product=ts3"),
                ("TeamSpeak Support", "https://support.teamspeak.com/")
            };

        LegalText.Text = _german
            ? "Diese Anwendung wird unabhängig entwickelt und steht in keiner geschäftlichen oder technischen Verbindung zu YACA Systems, TeamSpeak Systems GmbH oder anderen in den verlinkten Ressourcen genannten Drittanbietern. YACA und TeamSpeak sind eigenständige Produkte und Marken ihrer jeweiligen Rechteinhaber. Die Verwendung dieses Switchers erfolgt auf eigene Verantwortung. Prüfe vor Installations-, Backup-, Restore- oder Wechselvorgängen stets den vorgesehenen Zielpfad und stelle sicher, dass benötigte Programme nicht gleichzeitig auf die betroffenen Dateien zugreifen. Für Inhalte, Verfügbarkeit oder Änderungen externer Webseiten, Dienste und Downloads wird keine Gewähr übernommen. Externe Links öffnen sich in deinem Standardbrowser."
            : "This application is developed independently and has no business or technical affiliation with YACA Systems, TeamSpeak Systems GmbH, or other third parties referenced by the linked resources. YACA and TeamSpeak are independent products and trademarks of their respective rights holders. Use of this switcher is at your own responsibility. Before installation, backup, restore or switching operations, verify the intended target path and make sure required applications are not accessing the affected files. No warranty is provided for the content, availability or changes of external websites, services or downloads. External links open in your default browser.";
    }

    private static void Open(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // External link failures are intentionally isolated from the UI.
        }
    }

    private void Link_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string url })
            Open(url);
    }
}
