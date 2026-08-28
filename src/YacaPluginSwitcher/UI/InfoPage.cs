using System.Diagnostics;
using YacaPluginSwitcher.Configuration;

namespace YacaPluginSwitcher.UI;

public sealed class InfoPage : UserControl
{
    private readonly string _language;
    private bool IsGerman => _language == Localization.German;

    private static readonly (string Label, string Url)[] YacaLinks =
    [
        ("YACA Homepage", "https://yaca.systems/"),
        ("YACA Licenses", "https://yaca.systems/licenses"),
        ("YACA Terms of Service", "https://yaca.systems/tos"),
        ("YACA FAQ", "https://yaca.systems/faq"),
        ("YACA Discord", "https://discord.yaca.systems/")
    ];

    private static readonly (string Label, string Url)[] TeamSpeakLinks =
    [
        ("TeamSpeak Homepage", "https://www.teamspeak.com/"),
        ("TeamSpeak 3 Client", "https://teamspeak.com/de/downloads?product=ts3"),
        ("TeamSpeak Support", "https://support.teamspeak.com/")
    ];

    public InfoPage(string? language = null)
    {
        _language = Localization.Normalize(language);
        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        ForeColor = Theme.Foreground;
        Font = new Font("Segoe UI", 10F);
        BuildUi();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        Controls.Add(root);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        content.Controls.Add(BuildVendorCard("YACA", IsGerman ? "YACA stellt das Plugin bereit, das dieser Switcher verwaltet." : "YACA provides the plugin managed by this switcher.", YacaLinks, Theme.Accent), 0, 0);
        content.Controls.Add(BuildVendorCard("TEAMSpeak 3", IsGerman ? "Für dieses Tool ist ausdrücklich der TeamSpeak 3 Client erforderlich – nicht TeamSpeak 6." : "This tool specifically requires the TeamSpeak 3 client, not TeamSpeak 6.", TeamSpeakLinks, Theme.BrandGold), 1, 0);
        root.Controls.Add(content, 0, 0);

        var links = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Surface,
            Padding = new Padding(12, 10, 12, 8),
            Margin = Padding.Empty
        };
        AddBrandLink(links, Branding.DiscordIcon, "Community Discord", "https://discord.gg/9AxuZkyU7P");
        AddBrandLink(links, Branding.GitHubIcon, "GitHub Repository", "https://github.com/ViP3R76/Yaca-Plugin-Switcher");
        root.Controls.Add(links, 0, 1);

        root.Controls.Add(new Label
        {
            Text = IsGerman
                ? "YACA und TeamSpeak 3 sind Drittanbieter-Produkte. Diese Anwendung ist nicht mit YACA oder TeamSpeak Systems GmbH verbunden, wird nicht von ihnen unterstützt, gesponsert oder empfohlen."
                : "YACA and TeamSpeak 3 are third-party products. This application is not affiliated with, endorsed, sponsored, or recommended by YACA or TeamSpeak Systems GmbH.",
            Dock = DockStyle.Fill,
            ForeColor = Theme.SecondaryForeground,
            BackColor = Theme.Background,
            Font = new Font("Segoe UI", 8F),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = Padding.Empty,
            Padding = new Padding(12, 4, 12, 4)
        }, 0, 2);
    }

    private static Panel BuildVendorCard(string title, string description, IEnumerable<(string Label, string Url)> links, Color accent)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Margin = new Padding(6),
            Padding = new Padding(1)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(accent, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, card.Width - 1), Math.Max(0, card.Height - 1));
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Surface,
            Padding = new Padding(18, 16, 18, 16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 15F), ForeColor = accent, BackColor = Theme.Surface, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        root.Controls.Add(new Label { Text = description, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5F), ForeColor = Theme.SecondaryForeground, BackColor = Theme.Surface, AutoEllipsis = true, Padding = new Padding(0, 4, 0, 4) }, 0, 1);

        var linksPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 10, 0, 0),
            Margin = Padding.Empty
        };
        DarkMode.ApplyScrollBarTheme(linksPanel);
        foreach (var link in links)
        {
            var label = new LinkLabel
            {
                Text = link.Label,
                AutoSize = true,
                LinkColor = accent,
                ActiveLinkColor = Color.White,
                VisitedLinkColor = accent,
                BackColor = Theme.Surface,
                Margin = new Padding(0, 3, 0, 3),
                Cursor = Cursors.Hand
            };
            label.Click += (_, _) => OpenUrl(link.Url);
            linksPanel.Controls.Add(label);
        }
        root.Controls.Add(linksPanel, 0, 2);
        card.Controls.Add(root);
        return card;
    }

    private static void AddBrandLink(Control parent, Image icon, string text, string url)
    {
        var row = new Panel { Width = 330, Height = 34, BackColor = Theme.Surface, Margin = new Padding(0, 0, 18, 0) };
        row.Controls.Add(new PictureBox { Location = new Point(0, 5), Size = new Size(22, 22), SizeMode = PictureBoxSizeMode.Zoom, Image = icon, BackColor = Theme.Surface });
        var link = new LinkLabel { Text = text, Location = new Point(30, 6), AutoSize = true, LinkColor = Theme.Accent, ActiveLinkColor = Color.White, VisitedLinkColor = Theme.Accent, BackColor = Theme.Surface, Cursor = Cursors.Hand };
        link.Click += (_, _) => OpenUrl(url);
        row.Controls.Add(link);
        parent.Controls.Add(row);
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }
}
