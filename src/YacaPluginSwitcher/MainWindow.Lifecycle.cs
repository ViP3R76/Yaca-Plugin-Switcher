using System;

namespace YacaPluginSwitcher;

public partial class MainWindow : IDisposable
{
    public void Dispose()
    {
        _updaterCts?.Dispose();
        _updaterCts = null;
    }
}