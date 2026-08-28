using System.Security.Cryptography;

namespace YacaPluginSwitcher.Core;

public static class FileHashService
{
    public static string Sha256(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        using var stream = File.OpenRead(filePath);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
