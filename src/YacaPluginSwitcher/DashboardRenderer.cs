
    private void UpdateCurrentInstalledDetailsFromText(string text)
    {
        if (_currentDetailsPanel is null || _currentMetaText is null || _currentShaLabel is null || _currentShaValue is null) return;
        var lines = text.Split('\n', StringSplitOptions.None); if (lines.Length < 5 || string.IsNullOrWhiteSpace(lines[0])) { _currentDetailsPanel.Visibility = Visibility.Collapsed; return; }
        _currentDetailsPanel.Visibility = Visibility.Visible; _currentMetaText.Text = string.Join(Environment.NewLine, lines.Take(2)); _currentShaLabel.Text = lines[2].Trim(); _currentShaValue.Text = lines[4].Trim();
    }

    private void BuildTeamSpeakPanel(Grid host, int column)
    {
        var gold = (Brush)FindResource("GoldBrush"); var card = CreatePanelCard(gold); var panel = new Grid(); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetTeamSpeakStatus, "TEAMSPEAK STATUS", gold); Grid.SetRow(header, 0); panel.Children.Add(header);
        var center = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }; _tsStatus = new TextBlock { Text = "—", FontSize = 28, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Foreground = gold }; _tsDescription = new TextBlock { FontSize = 14, Foreground = (Brush)FindResource("SecondaryBrush"), TextWrapping = TextWrapping.NoWrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 10, 0, 0), MaxWidth = 420 }; center.Children.Add(_tsStatus); center.Children.Add(_tsDescription); Grid.SetRow(center, 1); panel.Children.Add(center);
        _tsClose = new Button { Content = IsGerman ? "TeamSpeak 3 schließen" : "Close TeamSpeak 3", Visibility = Visibility.Collapsed, Background = (Brush)FindResource("ErrorBrush"), Foreground = Brushes.White, BorderBrush = (Brush)FindResource("ErrorBrush"), BorderThickness = new Thickness(0), Padding = new Thickness(18, 8, 18, 8), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0), Cursor = System.Windows.Input.Cursors.Hand, FontSize = 17, FontWeight = FontWeights.Bold }; _tsClose.Template = CreateSquareButtonTemplate(); _tsClose.Click += (_, _) => CloseTeamSpeak(); Grid.SetRow(_tsClose, 2); panel.Children.Add(_tsClose); card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
    }

    private void BuildAvailableVersionsPanel(Grid host, int column)
    {
        var purple = (Brush)FindResource("AccentBrush"); var card = CreatePanelCard(purple); var panel = new Grid(); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetBackups, IsGerman ? "VERFÜGBARE VERSIONEN" : "AVAILABLE VERSIONS", purple); Grid.SetRow(header, 0); panel.Children.Add(header); _versionList = new StackPanel { Margin = new Thickness(6, 10, 6, 8) }; Grid.SetRow(_versionList, 1); panel.Children.Add(_versionList);