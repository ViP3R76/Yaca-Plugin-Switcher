namespace YacaPluginSwitcher.UI;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(8, 9, 13);
    public static readonly Color Sidebar = Color.FromArgb(10, 11, 16);
    public static readonly Color Surface = Color.FromArgb(15, 17, 23);
    public static readonly Color Control = Color.FromArgb(30, 31, 39);
    public static readonly Color ControlHover = Color.FromArgb(46, 29, 63);
    public static readonly Color NavSelected = Color.FromArgb(39, 20, 61);
    public static readonly Color Border = Color.FromArgb(69, 70, 82);
    public static readonly Color AccentDim = Color.FromArgb(88, 39, 122);
    public static readonly Color Foreground = Color.FromArgb(247, 247, 250);
    public static readonly Color SecondaryForeground = Color.FromArgb(185, 187, 198);
    public static readonly Color Accent = Color.FromArgb(181, 92, 255);
    public static readonly Color Success = Color.FromArgb(42, 221, 113);
    public static readonly Color Warning = Color.FromArgb(255, 205, 64);
    public static readonly Color Error = Color.FromArgb(255, 86, 71);
    public static readonly Color BrandGold = Color.FromArgb(252, 255, 79);

    public static void Apply(Control control)
    {
        control.BackColor = Background;
        control.ForeColor = Foreground;
    }

    public static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = ControlHover;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(62, 44, 74);
        button.BackColor = Control;
        button.ForeColor = Foreground;
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
    }

    public static void StyleComboBox(ComboBox comboBox)
    {
        comboBox.BackColor = Control;
        comboBox.ForeColor = Foreground;
        comboBox.FlatStyle = FlatStyle.Flat;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.DrawMode = DrawMode.OwnerDrawFixed;
        comboBox.ItemHeight = 24;
        comboBox.DrawItem += (_, e) =>
        {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= comboBox.Items.Count)
                return;
            var selected = (e.State & DrawItemState.Selected) != 0;
            using var brush = new SolidBrush(selected ? ControlHover : Control);
            using var textBrush = new SolidBrush(Foreground);
            e.Graphics.FillRectangle(brush, e.Bounds);
            var font = e.Font ?? comboBox.Font ?? SystemFonts.MessageBoxFont!;
            e.Graphics.DrawString(comboBox.Items[e.Index]?.ToString() ?? string.Empty, font, textBrush, e.Bounds.Left + 6, e.Bounds.Top + 3);
            e.DrawFocusRectangle();
        };
        comboBox.Cursor = Cursors.Hand;
    }

    public static void StyleListBox(ListBox listBox)
    {
        listBox.BackColor = Control;
        listBox.ForeColor = Foreground;
        listBox.BorderStyle = BorderStyle.FixedSingle;
        listBox.IntegralHeight = false;
    }

    public static void StyleListView(ListView listView)
    {
        listView.BackColor = Surface;
        listView.ForeColor = Foreground;
        listView.BorderStyle = BorderStyle.FixedSingle;
        listView.HideSelection = false;
    }
}
