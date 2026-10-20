using System.Text;

namespace Shortcuts.Services;

/// <summary>
/// Reads the target path of a shell shortcut (<c>.lnk</c>) without COM or System.Drawing.
/// </summary>
public static class LnkResolver
{
    private const int HeaderSize = 76;
    private const uint HasLinkInfo = 0x02;
    private const uint HasTargetIdList = 0x4000;
    private const uint IsUnicode = 0x40;

    /// <summary>
    /// Resolve the target of a shortcut. Returns <c>null</c> when the target cannot be read.
    /// </summary>
    public static string? Resolve(string lnkPath)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(lnkPath);
        }
        catch
        {
            return null;
        }

        if (bytes.Length < HeaderSize + 16 || bytes[0] != 0x4C)
            return null;

        var flags = ReadUInt32(bytes, 20);

        if ((flags & HasLinkInfo) != 0)
        {
            var data = HeaderSize;
            if ((flags & HasTargetIdList) != 0)
                data += 2 + (int)ReadUInt16(bytes, data);

            if (data + 16 <= bytes.Length && ReadUInt32(bytes, data + 4) == 1)
            {
                var idListLength = (int)ReadUInt32(bytes, data + 8);
                var offset = data + 12 + idListLength;
                if (offset + 4 <= bytes.Length)
                {
                    var text = ReadString(bytes, offset + 4, (int)ReadUInt32(bytes, offset), (flags & IsUnicode) != 0);
                    if (!string.IsNullOrWhiteSpace(text))
                        return text.TrimEnd('\0');
                }
            }
        }

        return ScanForPath(bytes, (flags & IsUnicode) != 0);
    }

    /// <summary>
    /// True when the path is a shell shortcut rather than the target itself.
    /// </summary>
    public static bool IsShortcut(string path) => path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);

    private static string? ScanForPath(byte[] bytes, bool unicode)
    {
        var attempts = 0;
        for (var i = HeaderSize; i + 3 < bytes.Length && attempts < 8; i++)
        {
            if (!LooksLikeDrive(bytes, i, unicode))
                continue;

            var text = ReadString(bytes, i, 520, unicode);
            if (text.Length <= 3)
                continue;

            attempts++;
            if (File.Exists(text))
                return text;
        }

        return null;
    }

    private static bool LooksLikeDrive(byte[] bytes, int i, bool unicode)
    {
        if (unicode)
            return i + 5 < bytes.Length
                && bytes[i] >= 'A' && (bytes[i] <= 'Z' || bytes[i] >= 'a' && bytes[i] <= 'z')
                && bytes[i + 1] == 0 && bytes[i + 2] == ':' && bytes[i + 3] == 0 && bytes[i + 4] == '\\';

        return i + 3 < bytes.Length
            && bytes[i] >= 'A' && (bytes[i] <= 'Z' || bytes[i] >= 'a' && bytes[i] <= 'z')
            && bytes[i + 1] == ':' && bytes[i + 2] == '\\';
    }

    private static string ReadString(byte[] bytes, int offset, int length, bool unicode)
    {
        if (offset < 0 || offset >= bytes.Length || length <= 0)
            return string.Empty;

        var end = Math.Min(offset + length, bytes.Length);
        var raw = bytes.AsSpan(offset, end - offset);

        var text = unicode
            ? Encoding.Unicode.GetString(raw)
            : Encoding.Latin1.GetString(raw);

        var stop = text.IndexOf('\0');
        return stop >= 0 ? text[..stop] : text;
    }

    private static uint ReadUInt32(byte[] bytes, int offset) =>
        offset + 4 > bytes.Length ? 0 : (uint)(bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24);

    private static uint ReadUInt16(byte[] bytes, int offset) =>
        offset + 2 > bytes.Length ? 0 : (uint)(bytes[offset] | bytes[offset + 1] << 8);
}
