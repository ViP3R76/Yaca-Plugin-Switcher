using System.Windows;
using System.Windows.Input;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    /// <summary>
    /// Prüft TeamSpeak und bietet bei laufendem Prozess das Schließen an.
    /// </summary>
    private void CloseTeamSpeak()
    {
        var text = Texts;

        if (!TeamSpeakDetector.IsRunning())
        {
            RefreshHome();
            return;
        }

        if (MessageBox.Show(
                text.CloseTeamspeakQuestion,
                text.TeamspeakRunningTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            if (!TeamSpeakDetector.TryCloseWithElevation(TimeSpan.FromSeconds(10)))
            {
                ShowError(text.CloseTeamspeakFailed);
            }
        }
        catch (Exception ex) when (
            ex is InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            ShowError(
                Localization.GetErrorMessage(
                    ex,
                    text,
                    text.CloseTeamspeakFailed));
        }
        finally
        {
            Mouse.OverrideCursor = null;
            RefreshHome();
        }
    }
}
