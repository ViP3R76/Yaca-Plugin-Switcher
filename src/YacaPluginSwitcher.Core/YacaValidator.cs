using System.Text;
using System.Text.RegularExpressions;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher.Core;

public static class YacaValidator
{
    private static readonly Regex VersionRegex = new(
        @"(?<!\d)(\d+)\.(\d+)\.(\d+)\.(\d{6,})(?!\d)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly byte[] YacaVoice = Encoding.ASCII.GetBytes("Yaca Voice");
    private static readonly byte[] YacaSystems = Encoding.ASCII.GetBytes("yaca systems");
    private static readonly byte[] VersionSymbol = Encoding.ASCII.GetBytes("fetchCurrentPluginVersion");
    private static readonly byte[] RequiredVersionSymbol = Encoding.ASCII.GetBytes("checkRequiredVersion");

    public static ValidationResult Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new(false, "Kein Dateipfad angegeben.");

        if (!File.Exists(path))
            return new(false, "Datei nicht gefunden.");

        long size;
        try
        {
            size = new FileInfo(path).Length;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new(false, $"Dateigröße konnte nicht gelesen werden: {ex.Message}");
        }

        if (!PeFileValidator.IsValidAmd64Dll(path, out var peMessage))
            return new(false, peMessage, FileSize: size);

        byte[] data;
        try
        {
            data = File.ReadAllBytes(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new(false, $"Datei konnte nicht gelesen werden: {ex.Message}", FileSize: size);
        }

        var signatureCount = 0;
        if (Contains(data, YacaVoice)) signatureCount++;
        if (Contains(data, YacaSystems)) signatureCount++;
        if (Contains(data, VersionSymbol)) signatureCount++;
        if (Contains(data, RequiredVersionSymbol)) signatureCount++;

        if (signatureCount < 3)
            return new(false, "Keine ausreichende YACA-Signatur erkannt.", FileSize: size);

        var text = Encoding.ASCII.GetString(data);
        var match = VersionRegex.Match(text);
        if (!match.Success || !Version.TryParse(
                $"{match.Groups[1].Value}.{match.Groups[2].Value}.{match.Groups[3].Value}",
                out var version))
        {
            return new(false, "YACA-Signatur erkannt, aber Version/Build konnte nicht bestimmt werden.", FileSize: size);
        }

        if (!long.TryParse(match.Groups[4].Value, out var build))
            return new(false, "YACA-Version erkannt, aber Build konnte nicht gelesen werden.", version, FileSize: size);

        string hash;
        try
        {
            hash = FileHashService.Sha256(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new(false, $"SHA-256 konnte nicht berechnet werden: {ex.Message}", version, build, size);
        }

        return new(true, "Gültige YACA x64-DLL erkannt.", version, build, size, hash);
    }

    private static bool Contains(byte[] data, byte[] needle)
    {
        if (needle.Length == 0 || needle.Length > data.Length)
            return false;

        for (var i = 0; i <= data.Length - needle.Length; i++)
        {
            if (data[i] != needle[0])
                continue;

            var match = true;
            for (var j = 1; j < needle.Length; j++)
            {
                if (data[i + j] == needle[j])
                    continue;

                match = false;
                break;
            }

            if (match)
                return true;
        }

        return false;
    }
}
