using System.Reflection;

namespace YacaPluginSwitcher.UI;

internal static class Branding
{
    private const string LogoResourceName = "YacaPluginSwitcher.Assets.yaca_logo.png";
    private const string DiscordIconResourceName = "YacaPluginSwitcher.Assets.discord_icon.png";
    private const string GitHubIconResourceName = "YacaPluginSwitcher.Assets.github_icon.png";
    private static Bitmap? _logo;
    private static Bitmap? _discordIcon;
    private static Bitmap? _githubIcon;

    public static Bitmap Logo => _logo ??= LoadResource(LogoResourceName);
    public static Bitmap DiscordIcon => _discordIcon ??= LoadResource(DiscordIconResourceName);
    public static Bitmap GitHubIcon => _githubIcon ??= LoadResource(GitHubIconResourceName);

    private static Bitmap LoadResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            throw new InvalidOperationException($"Embedded branding resource not found: {resourceName}");
        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    }
}
