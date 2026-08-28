using System.Reflection;

namespace YacaPluginSwitcher.UI;

internal static class Branding
{
    private const string LogoResourceName = "YacaPluginSwitcher.Assets.yaca_logo.png";
    private const string DiscordIconResourceName = "YacaPluginSwitcher.Assets.discord_icon.png";
    private const string GitHubIconResourceName = "YacaPluginSwitcher.Assets.github_icon.png";
    private static Image? _logo;
    private static Image? _discordIcon;
    private static Image? _githubIcon;

    public static Image Logo => _logo ??= LoadLogo();
    public static Image DiscordIcon => _discordIcon ??= LoadResourceIcon(DiscordIconResourceName);
    public static Image GitHubIcon => _githubIcon ??= LoadResourceIcon(GitHubIconResourceName);

    private static Bitmap LoadLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(LogoResourceName);
        if (stream is null)
            throw new InvalidOperationException($"Embedded branding resource not found: {LogoResourceName}");

        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    }
    private static Bitmap LoadResourceIcon(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            throw new InvalidOperationException($"Embedded branding resource not found: {resourceName}");

        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    }
}
