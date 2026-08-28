using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YacaPluginSwitcher.Core;

public static class TeamSpeakDetector
{
    private const uint WmClose = 0x0010;
    private static readonly string[] ProcessNames = ["ts3client_win64", "ts3client"];
    private static readonly EnumWindowsProc EnumWindowsCallback = CollectProcessWindow;

    public static bool IsRunning()
    {
        var processes = GetRunningProcesses();
        try
        {
            return processes.Count > 0;
        }
        finally
        {
            DisposeProcesses(processes);
        }
    }

    /// <summary>
    /// Requests a graceful shutdown of all detected TeamSpeak 3 client processes.
    /// The method never force-kills TeamSpeak.
    /// </summary>
    public static bool TryClose(TimeSpan waitTimeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(waitTimeout, TimeSpan.Zero);

        var processes = GetRunningProcesses();
        if (processes.Count == 0)
            return true;

        try
        {
            foreach (var process in processes)
                RequestGracefulClose(process);

            var deadline = DateTime.UtcNow + waitTimeout;
            while (DateTime.UtcNow < deadline)
            {
                if (!IsRunning())
                    return true;

                Thread.Sleep(100);
            }

            return !IsRunning();
        }
        finally
        {
            DisposeProcesses(processes);
        }
    }

    private static void RequestGracefulClose(Process process)
    {
        try
        {
            if (process.HasExited)
                return;

            process.Refresh();
            _ = process.CloseMainWindow();

            // CloseMainWindow() can return false for some TS3 builds even though
            // the client has a normal top-level window. Fall back to WM_CLOSE on
            // every top-level window owned by the process. This is still a graceful
            // close request; no process termination is performed.
            PostCloseToProcessWindows(process.Id);
        }
        catch (InvalidOperationException)
        {
            // Process exited between enumeration and the close request.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // The process may have exited or no longer allow inspection.
        }
    }

    private static void PostCloseToProcessWindows(int processId)
    {
        var state = new WindowSearchState(processId);
        var handle = GCHandle.Alloc(state);
        try
        {
            _ = EnumWindows(EnumWindowsCallback, GCHandle.ToIntPtr(handle));
        }
        finally
        {
            handle.Free();
        }
    }

    private static bool CollectProcessWindow(IntPtr hWnd, IntPtr lParam)
    {
        var handle = GCHandle.FromIntPtr(lParam);
        try
        {
            if (handle.Target is not WindowSearchState state)
                return true;

            _ = GetWindowThreadProcessId(hWnd, out var windowProcessId);
            if (windowProcessId != (uint)state.ProcessId || !IsWindowVisible(hWnd))
                return true;

            _ = PostMessageW(hWnd, WmClose, IntPtr.Zero, IntPtr.Zero);
            return true;
        }
        finally
        {
            // EnumWindows invokes the callback repeatedly, so the GCHandle must
            // remain valid until enumeration has completed. It is freed by the
            // caller after EnumWindows returns.
        }
    }

    private static List<Process> GetRunningProcesses()
    {
        var result = new List<Process>();

        foreach (var processName in ProcessNames)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    if (process.HasExited)
                    {
                        process.Dispose();
                        continue;
                    }

                    result.Add(process);
                }
                catch (InvalidOperationException)
                {
                    process.Dispose();
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    process.Dispose();
                }
            }
        }

        return result;
    }

    private static void DisposeProcesses(IEnumerable<Process> processes)
    {
        foreach (var process in processes)
            process.Dispose();
    }

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", ExactSpelling = true, EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private sealed class WindowSearchState
    {
        public WindowSearchState(int processId) => ProcessId = processId;

        public int ProcessId { get; }
    }
}
