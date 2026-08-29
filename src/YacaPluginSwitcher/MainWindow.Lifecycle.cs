using System;

namespace YacaPluginSwitcher;

public partial class MainWindow : IDisposable
{
    public void Dispose()
    {
        _updaterCts?.Cancel();
        _updaterCts?.Dispose();
        _updaterCts = null;
        _flashTimer?.Stop();
        GC.SuppressFinalize(this);
    }
}