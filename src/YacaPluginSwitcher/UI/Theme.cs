namespace YacaPluginSwitcher.UI;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(24, 26, 30);
    public static readonly Color Surface = Color.FromArgb(30, 33, 38);
    public static readonly Color Control = Color.FromArgb(42, 45, 52);
    public static readonly Color ControlHover = Color.FromArgb(58, 48, 70);
    public static readonly Color Foreground = Color.FromArgb(245, 245, 245);
    public static readonly Color SecondaryForeground = Color.FromArgb(190, 194, 202);
    public static readonly Color Accent = Color.FromArgb(181, 92, 255);
    public static readonly Color Success = Color.LightGreen;
    public static readonly Color Warning = Color.Gold;
    public static readonly Color Error = Color.OrangeRed;
    public static readonly Color BrandGold = Color.FromArgb(252, 255, 79);

    public static void Apply(Control control)
    {
        control.BackColor = Background;
        control.ForeColor = Foreground;
    }

    public static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(103, 65, 135);
        button.FlatAppearance.MouseOverBackColor = ControlHover;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(62, 66, 74);
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
            var background = selected ? ControlHover : Control;
            var foreground = Foreground;
            using var brush = new SolidBrush(background);
            using var textBrush = new SolidBrush(foreground);
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
