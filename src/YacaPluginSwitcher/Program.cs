using System.Diagnostics;
using YacaPluginSwitcher.Configuration;

namespace YacaPluginSwitcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, "Local\\ViP3r76.YacaPluginSwitcher", out var createdNew);
        var detectedText = Localization.Get(Localization.DetectSystemLanguage());
        if (!createdNew)
        {
            MessageBox.Show(
                detectedText.AlreadyRunningMessage,
                detectedText.Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => ShowFatalError(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                ShowFatalError(ex);
        };

        try
        {
            var service = StartupForm.CreateService(out var startupException);
            if (startupException is not null || service is null)
            {
                ShowFatalError(startupException ?? new InvalidOperationException("YACA service initialization failed."));
                return;
            }

            Application.Run(new MainForm(service));
        }
        catch (Exception ex)
        {
            ShowFatalError(ex);
        }
    }

    private static void ShowFatalError(Exception exception)
    {
        try
        {
            var text = Localization.Get(Localization.DetectSystemLanguage());
            MessageBox.Show(
                $"{text.StartErrorMessage}\n\n{text.TechnicalDetails}: {exception.GetType().Name}: {exception.Message}",
                text.StartErrorTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            Debug.WriteLine(exception);
        }
    }
}
