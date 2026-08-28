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
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            BackColor = Theme.Background, Padding = new Padding(0, 4, 0, 0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        Controls.Add(root);

        var intro = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.Surface, Margin = new Padding(0, 0, 0, 10), Padding = new Padding(16, 8, 16, 8) };
        intro.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        intro.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        intro.Controls.Add(new Label
        {
            Text = "YACA Plugin Switcher", Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 19F), ForeColor = Theme.Accent,
            BackColor = Theme.Surface, TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        intro.Controls.Add(new Label
        {
            Text = $"v{Application.ProductVersion}  •  ViP3R_76",
            Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F), ForeColor = Theme.BrandGold,
            BackColor = Theme.Surface, TextAlign = ContentAlignment.MiddleRight
        }, 1, 0);
        root.Controls.Add(intro, 0, 0);

        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.Background, Margin = Padding.Empty };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        content.Controls.Add(BuildVendorCard("YACA", IsGerman ? "YACA ist die Grundlage dieses Plugin-Workflows." : "YACA provides the plugin used by this workflow.", YacaLinks, Theme.Accent), 0, 0);
        content.Controls.Add(BuildVendorCard("TEAMSpeak 3", IsGerman ? "Für dieses Tool ist ausdrücklich der TeamSpeak 3 Client erforderlich – nicht TeamSpeak 6." : "This tool specifically requires the TeamSpeak 3 client, not TeamSpeak 6.", TeamSpeakLinks, Theme.BrandGold), 1, 0);
        root.Controls.Add(content, 0, 1);

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Surface, Padding = new Padding(16, 10, 16, 10) };
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var links = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Theme.Surface, Padding = Padding.Empty, Margin = Padding.Empty };
        AddBrandLink(links, Branding.DiscordIcon, "SnakeNest Community - by ViP3R_76", "https://discord.gg/9AxuZkyU7P");
        AddBrandLink(links, Branding.GitHubIcon, "GitHub Repository", "https://github.com/ViP3R76/Yaca-Plugin-Switcher");
        footer.Controls.Add(links, 0, 0);
        footer.Controls.Add(new Label
        {
            Text = IsGerman
                ? "YACA und TeamSpeak 3 sind Drittanbieter-Produkte. Diese Anwendung ist nicht mit YACA oder TeamSpeak Systems GmbH verbunden, wird nicht von ihnen unterstützt, gesponsert oder empfohlen."
                : "YACA and TeamSpeak 3 are third-party products. This application is not affiliated with, endorsed, sponsored, or recommended by YACA or TeamSpeak Systems GmbH.",
            Dock = DockStyle.Fill, ForeColor = Theme.SecondaryForeground, BackColor = Theme.Surface, Font = new Font("Segoe UI", 8F), AutoEllipsis = true
        }, 0, 1);
        root.Controls.Add(footer, 0, 2);
    }

    private static Panel BuildVendorCard(string title, string description, IEnumerable<(string Label, string Url)> links, Color accent)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Margin = new Padding(5), Padding = new Padding(1) };
        card.Paint += (_, e) => { using var pen = new Pen(accent, 1); e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, card.Width - 1), Math.Max(0, card.Height - 1)); };
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.Surface, Padding = new Padding(16, 14, 16, 14) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 14F), ForeColor = accent, BackColor = Theme.Surface, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        root.Controls.Add(new Label { Text = description, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F), ForeColor = Theme.SecondaryForeground, BackColor = Theme.Surface, AutoEllipsis = true }, 0, 1);
        var linksPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Theme.Surface, Padding = new Padding(0, 10, 0, 0) };
        foreach (var link in links)
        {
            var label = new LinkLabel { Text = link.Label, AutoSize = true, LinkColor = Theme.Accent, ActiveLinkColor = Color.White, VisitedLinkColor = Theme.Accent, BackColor = Theme.Surface, Margin = new Padding(0, 2, 0, 2), Cursor = Cursors.Hand };
            label.Click += (_, _) => OpenUrl(link.Url);
            linksPanel.Controls.Add(label);
        }
        root.Controls.Add(linksPanel, 0, 2); card.Controls.Add(root); return card;
    }

    private static void AddBrandLink(Control parent, Image icon, string text, string url)
    {
        var row = new Panel { Width = 330, Height = 32, BackColor = Theme.Surface, Margin = new Padding(0, 0, 18, 0) };
        row.Controls.Add(new PictureBox { Location = new Point(0, 4), Size = new Size(22, 22), SizeMode = PictureBoxSizeMode.Zoom, Image = icon, BackColor = Theme.Surface });
        var link = new LinkLabel { Text = text, Location = new Point(30, 5), AutoSize = true, LinkColor = Theme.Accent, ActiveLinkColor = Color.White, VisitedLinkColor = Theme.Accent, BackColor = Theme.Surface, Cursor = Cursors.Hand };
        link.Click += (_, _) => OpenUrl(url); row.Controls.Add(link); parent.Controls.Add(row);
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }
}
