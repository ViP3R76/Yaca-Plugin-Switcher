using System.Diagnostics;
using YacaPluginSwitcher.Configuration;
using YacaPluginSwitcher.Core;

namespace YacaPluginSwitcher;

public partial class App : Application
{
    private Mutex? _mutex;
    public static YacaService? Service { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _mutex = new Mutex(true, "Local\\ViP3r76.YacaPluginSwitcher", out var createdNew);
        if (!createdNew)
        {
            var text = Localization.Get(Localization.DetectSystemLanguage());
            MessageBox.Show(text.AlreadyRunningMessage, text.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        DispatcherUnhandledException += (_, args) => { ShowFatalError(args.Exception); args.Handled = true; Shutdown(1); };
        AppDomain.CurrentDomain.UnhandledException += (_, args) => { if (args.ExceptionObject is Exception ex) ShowFatalError(ex); };
        new StartupWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.Dispose();
        base.OnExit(e);
    }

    internal static void SetService(YacaService service) => Service = service;

    internal static void ShowFatalError(Exception exception)
    {
        try
        {
            var text = Localization.Get(Localization.DetectSystemLanguage());
            MessageBox.Show($"{text.StartErrorMessage}\n\n{text.TechnicalDetails}: {exception.GetType().Name}: {exception.Message}", text.StartErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch { Debug.WriteLine(exception); }
    }
}