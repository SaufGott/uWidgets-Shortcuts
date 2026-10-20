namespace Shortcuts.Services;

/// <summary>
/// Stroke glyphs used when the shell has no icon for an entry.
/// Path data is drawn with a stroke, so it follows the theme foreground (which is the
/// accent colour when the monochrome theme is active).
/// </summary>
public static class Glyphs
{
    private const string App = "M4,4 L20,4 L20,20 L4,20 Z M4,7.5 L20,7.5 M7,12 L13,12 M7,15 L16,15";
    private const string Folder = "M3,6 L8.5,6 L10.5,8.5 L21,8.5 L21,19 L3,19 Z";
    private const string Document = "M6,3 L14,3 L18,7 L18,21 L6,21 Z M14,3 L14,7 L18,7 M9,12 L15,12 M9,15 L15,15";
    private const string Url = "M12,3 A9,9 0 1,0 12,21 A9,9 0 1,0 12,3 Z M3,12 L21,12 M12,3 C9.5,8 9.5,16 12,21 M12,3 C14.5,8 14.5,16 12,21";
    private const string Unknown = "M12,3 A9,9 0 1,0 12,21 A9,9 0 1,0 12,3 Z";

    /// <summary>
    /// Pick a glyph for a target based on what it looks like.
    /// </summary>
    public static string For(string targetPath)
    {
        if (targetPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            targetPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return Url;

        if (Directory.Exists(targetPath))
            return Folder;

        var extension = Path.GetExtension(targetPath).ToLowerInvariant();
        return extension switch
        {
            ".exe" or ".lnk" or ".bat" or ".cmd" or ".msi" or ".com" => App,
            ".txt" or ".md" or ".pdf" or ".doc" or ".docx" or ".rtf" or ".log" => Document,
            _ => Unknown
        };
    }
}
