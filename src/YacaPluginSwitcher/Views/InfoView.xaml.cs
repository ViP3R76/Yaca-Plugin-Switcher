using System.Diagnostics;

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
        YacaDescription.Text = _german
            ? "YACA ist das Plugin, das dieser Switcher verwaltet. Die folgenden offiziellen Ressourcen enthalten Informationen, Lizenzen, Nutzungsbedingungen und Hilfe."
            : "YACA is the plugin managed by this switcher. The official resources below provide information, licenses, terms of service and help.";

        TsDescription.Text = _german
            ? "Für dieses Tool wird ausdrücklich der TeamSpeak 3 Client benötigt – nicht TeamSpeak 6. Hier findest du die offiziellen TeamSpeak-Ressourcen und den Support."
            : "This tool specifically requires the TeamSpeak 3 client – not TeamSpeak 6. The official TeamSpeak resources and support are listed below.";

        YacaLinks.ItemsSource = new[]
        {
            ("YACA Homepage", "https://yaca.systems/"),
            ("YACA Licenses", "https://yaca.systems/licenses"),
            ("YACA Terms of Service", "https://yaca.systems/tos"),
            ("YACA FAQ", "https://yaca.systems/faq"),
            ("YACA Discord", "https://discord.yaca.systems/")
        };

        TsLinks.ItemsSource = new[]
        {
            ("TeamSpeak Homepage", "https://www.teamspeak.com/"),
            ("TeamSpeak 3 Client", "https://teamspeak.com/de/downloads?product=ts3"),
            ("TeamSpeak Support", "https://support.teamspeak.com/")
        };

        DiscordTitle.Text = _german ? "Mein Discord" : "My Discord";
        DiscordSubtitle.Text = _german ? "Community & Support" : "Community & support";
        GitHubTitle.Text = _german ? "GitHub Repository" : "GitHub repository";
        GitHubSubtitle.Text = _german ? "YACA Plugin Switcher" : "YACA Plugin Switcher";

        LegalText.Text = _german
            ? "Hinweis: YACA und TeamSpeak 3 sind Drittanbieter-Produkte. Diese Anwendung ist nicht mit YACA oder TeamSpeak Systems GmbH verbunden, wird nicht von ihnen unterstützt, gesponsert oder empfohlen."
            : "Disclaimer: YACA and TeamSpeak 3 are third-party products. This application is not affiliated with, endorsed, sponsored, or recommended by YACA or TeamSpeak Systems GmbH.";
    }

    private static void Open(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // External browser launch is best-effort and must not crash the application.
        }
    }

    private void Link_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string url)
            Open(url);
    }

    private void Discord_Click(object sender, RoutedEventArgs e) => Open("https://discord.gg/9AxuZkyU7P");

    private void GitHub_Click(object sender, RoutedEventArgs e) => Open("https://github.com/ViP3R76/Yaca-Plugin-Switcher");
}