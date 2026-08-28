using System.Runtime.InteropServices;

namespace YacaPluginSwitcher.UI;

internal static class DarkMode
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeLegacy = 19;

    public static void Apply(Form form)
    {
        ArgumentNullException.ThrowIfNull(form);

        try
        {
            form.HandleCreated += (_, _) => ApplyToHandle(form.Handle);
            if (form.IsHandleCreated)
                ApplyToHandle(form.Handle);
        }
        catch (DllNotFoundException)
        {
            // The application is Windows-only; this is a defensive fallback for unusual environments.
        }
        catch (EntryPointNotFoundException)
        {
            // Older Windows builds may not expose the DWM entry point.
        }
    }

    public static void ApplyScrollBarTheme(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);

        void Apply()
        {
            if (!control.IsHandleCreated)
                return;

            try
            {
                _ = SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
            }
            catch (DllNotFoundException)
            {
                // Defensive fallback for unusual Windows environments.
            }
            catch (EntryPointNotFoundException)
            {
                // Older Windows builds may not expose SetWindowTheme.
            }
        }

        control.HandleCreated += (_, _) => Apply();
        if (control.IsHandleCreated)
            Apply();
    }

    private static void ApplyToHandle(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            return;

        try
        {
            var dark = 1;
            var result = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int));
            if (result != 0)
                _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeLegacy, ref dark, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // Defensive fallback for unusual Windows environments.
        }
        catch (EntryPointNotFoundException)
        {
            // Older Windows builds may not expose the requested DWM attribute.
        }
    }

    [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string? pszSubIdList);

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}
