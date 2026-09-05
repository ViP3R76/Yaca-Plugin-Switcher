using YacaPluginSwitcher.Core;

namespace YacaPluginSwitcher;

public partial class StartupWindow : Window
{
    private readonly UiText _text;

    public StartupWindow()
    {
        InitializeComponent();
        _text = Localization.Get(Localization.DetectSystemLanguage());
        StatusText.Text = _text.StartupStarting;
        StartupProgressBar.Value = 0;
        Loaded += StartupWindow_Loaded;
    }

    private async void StartupWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = _text.StartupLoading;
            StartupProgressBar.Value = 20;
            await Task.Yield();

            App.SetService(await Task.Run(() => new YacaService()));
            StartupProgressBar.Value = 90;

            StatusText.Text = _text.StartupReady;
            StartupProgressBar.Value = 100;
            await Task.Delay(150);

            new MainWindow(App.Service!).Show();
            Close();
        }
        catch (Exception ex)
        {
            App.ShowFatalError(ex);
            Application.Current.Shutdown(1);
        }
    }
}
