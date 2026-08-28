using System.Diagnostics;
using YacaPluginSwitcher.Configuration;

namespace YacaPluginSwitcher.UI;

public sealed class AboutForm : Form
{
    private const int FormWidth = 680;
    private readonly string _language;

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
        ("TeamSpeak Support", "https://support.teamspeak.com/"),
        ("TeamSpeak 3 Plugin Information", "https://support.teamspeak.com/hc/en-us/articles/360002712358-How-do-I-write-my-own-plugins"),
        ("TeamSpeak Licensing", "https://support.teamspeak.com/hc/en-us/sections/360000716518-Licenses")
    ];

    public AboutForm(string? language = null)
    {
        _language = Localization.Normalize(language);
        var text = Localization.Get(_language);
        Text = text.AboutTitle;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(FormWidth, 520);
        MinimumSize = new Size(FormWidth, 420);
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Background;
        ForeColor = Theme.Foreground;
        DarkMode.Apply(this);

        BuildUi(text);
        ResizeToContent();
    }

    private void BuildUi(UiText text)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 20, 24, 18),
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        Controls.Add(root);

        var content = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            AutoSize = true,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground,
            Padding = new Padding(0)
        };
        root.Controls.Add(content, 0, 0);

        var header = new TableLayoutPanel
        {
            Width = FormWidth - 48,
            Height = 64,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground,
            Margin = new Padding(0, 0, 0, 12),
            Padding = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        header.Controls.Add(new Label
        {
            Text = "YACA Plugin Switcher",
            Dock = DockStyle.Fill,
            AutoSize = false,
            Font = new Font("Segoe UI Semibold", 20F),
            ForeColor = Theme.Accent,
            BackColor = Theme.Background,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        header.Controls.Add(new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = Branding.Logo,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        }, 1, 0);
        content.Controls.Add(header);

        content.Controls.Add(BuildDetails(
            _language == Localization.German
                ? $"Version {Application.ProductVersion}\nAutor: ViP3R_76\n\nEin unabhängiges Drittanbieter-Werkzeug zum Verwalten und Wechseln von YACA-TeamSpeak-3-Plugins."
                : $"Version {Application.ProductVersion}\nAuthor: ViP3R_76\n\nAn independent third-party utility for managing and switching YACA TeamSpeak 3 plugins."));
        content.Controls.Add(BlockWithLinks("YACA", YacaLinks));
        content.Controls.Add(BlockWithLinks("TEAMSpeak 3", TeamSpeakLinks));
        content.Controls.Add(BlockWithBrandLinks(
            text.Community,
            [
                (Branding.DiscordIcon, "SnakeNest Community - by ViP3R_76", "https://discord.gg/9AxuZkyU7P"),
                (Branding.GitHubIcon, text.GitHubRepository, "https://github.com/ViP3R76/Yaca-Plugin-Switcher")
            ],
            _language == Localization.German
                ? "YACA und TeamSpeak 3 sind Drittanbieter-Produkte. Diese Anwendung ist nicht mit YACA oder TeamSpeak Systems GmbH verbunden, wird nicht von ihnen unterstützt, gesponsert oder empfohlen. Alle Marken und Rechte verbleiben bei ihren jeweiligen Eigentümern.\n\n© 2026 ViP3R_76"
                : "YACA and TeamSpeak 3 are third-party products. This application is not affiliated with, endorsed, sponsored, or recommended by YACA or TeamSpeak Systems GmbH. All trademarks and rights remain with their respective owners.\n\n© 2026 ViP3R_76"));

        var close = new Button
        {
            Text = text.Close,
            Width = 110,
            Height = 36,
            Anchor = AnchorStyles.None,
            Margin = new Padding(0)
        };
        Theme.StyleButton(close);
        close.Click += (_, _) => Close();
        var closePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Theme.Background
        };
        closePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        closePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        closePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        closePanel.Controls.Add(close, 1, 0);
        root.Controls.Add(closePanel, 0, 1);
    }

    private static Panel BuildDetails(string body)
    {
        var panel = new Panel
        {
            Width = FormWidth - 48,
            Height = 104,
            BackColor = Theme.Background,
            Margin = new Padding(0, 0, 0, 12)
        };
        panel.Controls.Add(new Label
        {
            Text = body,
            Dock = DockStyle.Fill,
            ForeColor = Theme.Foreground,
            BackColor = Theme.Background,
            AutoSize = false
        });
        return panel;
    }

    private FlowLayoutPanel BlockWithLinks(string heading, IEnumerable<(string Label, string Url)> links, string? footer = null)
    {
        var panel = new FlowLayoutPanel
        {
            Width = FormWidth - 48,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            AutoSize = true,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(0)
        };
        panel.Controls.Add(new Label
        {
            Text = heading,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10F),
            ForeColor = Theme.SecondaryForeground,
            BackColor = Theme.Background,
            Margin = new Padding(0, 0, 0, 6)
        });

        foreach (var link in links)
        {
            var linkLabel = new LinkLabel
            {
                Text = link.Label,
                AutoSize = true,
                LinkColor = Theme.Accent,
                ActiveLinkColor = Color.White,
                VisitedLinkColor = Theme.Accent,
                BackColor = Theme.Background,
                Margin = new Padding(0, 1, 0, 1),
                Cursor = Cursors.Hand
            };
            linkLabel.Click += (_, _) => OpenUrl(link.Url);
            panel.Controls.Add(linkLabel);
        }

        if (!string.IsNullOrWhiteSpace(footer))
        {
            panel.Controls.Add(new Label
            {
                Text = footer,
                Width = FormWidth - 48,
                AutoSize = true,
                MaximumSize = new Size(FormWidth - 48, 0),
                ForeColor = Theme.SecondaryForeground,
                BackColor = Theme.Background,
                Margin = new Padding(0, 10, 0, 0)
            });
        }

        return panel;
    }

    private FlowLayoutPanel BlockWithBrandLinks(string heading, IEnumerable<(Image Icon, string Label, string Url)> links, string? footer = null)
    {
        var panel = new FlowLayoutPanel
        {
            Width = FormWidth - 48,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            AutoSize = true,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(0)
        };
        panel.Controls.Add(new Label
        {
            Text = heading,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10F),
            ForeColor = Theme.SecondaryForeground,
            BackColor = Theme.Background,
            Margin = new Padding(0, 0, 0, 6)
        });

        foreach (var link in links)
        {
            var row = new Panel
            {
                Width = FormWidth - 48,
                Height = 30,
                BackColor = Theme.Background,
                Margin = new Padding(0, 1, 0, 1)
            };
            var icon = new PictureBox
            {
                Location = new Point(0, 3),
                Size = new Size(22, 22),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = link.Icon,
                BackColor = Theme.Background
            };
            var linkLabel = new LinkLabel
            {
                Text = link.Label,
                Location = new Point(30, 3),
                AutoSize = true,
                LinkColor = Theme.Accent,
                ActiveLinkColor = Color.White,
                VisitedLinkColor = Theme.Accent,
                BackColor = Theme.Background,
                Cursor = Cursors.Hand
            };
            linkLabel.Click += (_, _) => OpenUrl(link.Url);
            row.Controls.Add(icon);
            row.Controls.Add(linkLabel);
            panel.Controls.Add(row);
        }

        if (!string.IsNullOrWhiteSpace(footer))
        {
            panel.Controls.Add(new Label
            {
                Text = footer,
                Width = FormWidth - 48,
                AutoSize = true,
                MaximumSize = new Size(FormWidth - 48, 0),
                ForeColor = Theme.SecondaryForeground,
                BackColor = Theme.Background,
                Margin = new Padding(0, 10, 0, 0)
            });
        }

        return panel;
    }

    private void ResizeToContent()
    {
        if (Controls.Count == 0 || Controls[0] is not TableLayoutPanel root || root.Controls.Count == 0)
            return;

        root.PerformLayout();
        var content = root.Controls[0];
        var desiredHeight = content.PreferredSize.Height + 20 + 44 + 18;
        var workingArea = Screen.FromControl(this).WorkingArea;
        var maximumHeight = Math.Max(MinimumSize.Height, workingArea.Height - 40);
        ClientSize = new Size(FormWidth, Math.Min(Math.Max(MinimumSize.Height, desiredHeight), maximumHeight));
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            var text = Localization.Get(_language);
            MessageBox.Show(this, text.LinkOpenFailed, text.LinkTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
