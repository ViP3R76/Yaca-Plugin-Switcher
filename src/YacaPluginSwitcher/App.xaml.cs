using System.Diagnostics;
using YacaPluginSwitcher.Core;

namespace YacaPluginSwitcher;

public partial class App : Application
{
    public static YacaService? Service { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Any(arg => string.Equals(arg, "--close-teamspeak", StringComparison.OrdinalIgnoreCase)))
        {
            Shutdown(TeamSpeakDetector.TryClose(TimeSpan.FromSeconds(8)) ? 0 : 1);
            return;
        }

        var mutex = new Mutex(true, "Local\\ViP3r76.YacaPluginSwitcher", out var createdNew);
        Properties["SingleInstanceMutex"] = mutex;
        if (!createdNew)
        {
            var text = Localization.Get(Localization.DetectSystemLanguage());
            MessageBox.Show(text.AlreadyRunningMessage, text.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            ShowFatalError(args.Exception);
            args.Handled = true;
            Shutdown(1);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                ShowFatalError(ex);
        };
        new StartupWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Properties["SingleInstanceMutex"] is IDisposable mutex)
            mutex.Dispose();
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
        catch
        {
            Debug.WriteLine(exception);
        }
    }
}
