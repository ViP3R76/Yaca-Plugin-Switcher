using System.Diagnostics;
namespace YacaPluginSwitcher;
public partial class InfoView : UserControl
{
    private readonly bool _german;
    public InfoView(string? language){_german=Localization.Normalize(language)==Localization.German;InitializeComponent();Build();}
    private void Build()
    {
        PageSubtitle.Text=_german?"Wichtige Informationen, offizielle Seiten und Community-Verbindungen.":"Important information, official resources and community links.";
        YacaDescription.Text=_german?"YACA stellt das Plugin bereit, das dieser Switcher verwaltet. Hier findest du offizielle Informationen, Lizenzbedingungen und Hilfestellungen rund um YACA.":"YACA provides the plugin managed by this switcher. Find official information, licensing terms and help resources for YACA here.";
        TsDescription.Text=_german?"Für dieses Tool ist ausdrücklich der TeamSpeak 3 Client erforderlich – nicht TeamSpeak 6. Die folgenden Links führen direkt zu den offiziellen TeamSpeak-Ressourcen.":"This tool specifically requires the TeamSpeak 3 client, not TeamSpeak 6. The following links lead directly to official TeamSpeak resources.";
        CommunityDescription.Text=_german?"Bleibe auf dem Laufenden, tausche dich mit der Community aus und finde die Projekte hinter YACA Plugin Switcher.":"Stay up to date, connect with the community and discover the projects behind YACA Plugin Switcher.";
        DiscordLinkText.Text="SnakeNest Community · Discord";
        TwitchLinkText.Text="ViP3R_76 · Twitch";
        GitHubLinkText.Text="YACA Plugin Switcher · GitHub";
        YacaLinks.ItemsSource=_german?new[]{("YACA Homepage","https://yaca.systems/"),("YACA Lizenzen","https://yaca.systems/licenses"),("YACA Nutzungsbedingungen","https://yaca.systems/tos"),("YACA FAQ","https://yaca.systems/faq"),("YACA Discord","https://discord.yaca.systems/")}:new[]{("YACA Homepage","https://yaca.systems/"),("YACA Licenses","https://yaca.systems/licenses"),("YACA Terms of Service","https://yaca.systems/tos"),("YACA FAQ","https://yaca.systems/faq"),("YACA Discord","https://discord.yaca.systems/")};
        TsLinks.ItemsSource=_german?new[]{("TeamSpeak Homepage","https://www.teamspeak.com/"),("TeamSpeak 3 Client","https://teamspeak.com/de/downloads?product=ts3"),("TeamSpeak Support","https://support.teamspeak.com/")}:new[]{("TeamSpeak Homepage","https://www.teamspeak.com/"),("TeamSpeak 3 Client","https://teamspeak.com/en/downloads?product=ts3"),("TeamSpeak Support","https://support.teamspeak.com/")};
        LegalText.Text=_german?"YACA und TeamSpeak 3 sind Drittanbieter-Produkte. Diese Anwendung ist nicht mit YACA oder TeamSpeak Systems GmbH verbunden, wird nicht von ihnen unterstützt, gesponsert oder empfohlen.":"YACA and TeamSpeak 3 are third-party products. This application is not affiliated with, endorsed, sponsored, or recommended by YACA or TeamSpeak Systems GmbH.";
    }
    private static void Open(string url){try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}catch{}}
    private void Link_Click(object sender,RoutedEventArgs e){if(sender is Button b&&b.Tag is string url)Open(url);}
    private void Discord_Click(object s,RoutedEventArgs e)=>Open("https://discord.gg/9AxuZkyU7P");
    private void GitHub_Click(object s,RoutedEventArgs e)=>Open("https://github.com/ViP3R76/Yaca-Plugin-Switcher");
}