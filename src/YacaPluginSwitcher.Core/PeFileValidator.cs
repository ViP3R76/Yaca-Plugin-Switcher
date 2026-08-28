namespace YacaPluginSwitcher.Core;

public static class PeFileValidator
{
    private const ushort MachineAmd64 = 0x8664;
    private const ushort ImageFileDll = 0x2000;
    private const ushort DosSignature = 0x5A4D;
    private const uint PeSignature = 0x00004550;

    public static bool IsValidAmd64Dll(string path, out string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        message = string.Empty;

        try
        {
            using var stream = File.OpenRead(path);
            if (stream.Length < 0x40)
            {
                message = "Die Datei ist zu klein für einen gültigen PE-Header.";
                return false;
            }

            using var reader = new BinaryReader(stream);
            if (reader.ReadUInt16() != DosSignature)
            {
                message = "Kein gültiges MZ/PE-Image.";
                return false;
            }

            stream.Position = 0x3C;
            var peOffset = reader.ReadInt32();
            if (peOffset < 0 || peOffset > stream.Length - 24)
            {
                message = "Ungültiger PE-Header.";
                return false;
            }

            stream.Position = peOffset;
            if (reader.ReadUInt32() != PeSignature)
            {
                message = "Ungültige PE-Signatur.";
                return false;
            }

            var machine = reader.ReadUInt16();
            if (machine != MachineAmd64)
            {
                message = "Die DLL ist keine 64-Bit-x64-DLL.";
                return false;
            }

            _ = reader.ReadUInt16(); // NumberOfSections
            _ = reader.ReadUInt32(); // TimeDateStamp
            _ = reader.ReadUInt32(); // PointerToSymbolTable
            _ = reader.ReadUInt32(); // NumberOfSymbols
            _ = reader.ReadUInt16(); // SizeOfOptionalHeader
            var characteristics = reader.ReadUInt16();

            if ((characteristics & ImageFileDll) == 0)
            {
                message = "Die Datei ist keine Windows-DLL.";
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or EndOfStreamException or ArgumentException)
        {
            message = $"PE-Prüfung fehlgeschlagen: {ex.Message}";
            return false;
        }
    }
}
