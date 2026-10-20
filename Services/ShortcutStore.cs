using System;
using System.Reflection;

namespace Shortcuts.Services;

/// <summary>
/// Copies a dropped shortcut into the widget's own folder. An entry that points at a desktop
/// shortcut breaks when that shortcut is deleted, so the stored copy is what the widget uses.
/// </summary>
public static class ShortcutStore
{
    // The plugin is loaded from uWidgets' Widgets folder, so its own directory is that folder.
    // Same as uWidgets.Core.Const.WidgetsFolder, without depending on a type the pinned mirror
    // of Core does not carry.
    private static string Folder => Path.Combine(
        Path.GetDirectoryName(typeof(ShortcutStore).Assembly.Location)!, "shortcuts");

    /// <summary>
    /// Returns the path the entry should use. Only <c>.lnk</c> files are copied — a path that
    /// already points at the program itself is left alone. Falls back to the original path if
    /// the copy fails.
    /// </summary>
    public static string Store(string path)
    {
        if (!LnkResolver.IsShortcut(path))
            return path;

        if (path.StartsWith(Folder, StringComparison.OrdinalIgnoreCase))
            return path;

        try
        {
            Directory.CreateDirectory(Folder);

            var name = Sanitise(Path.GetFileNameWithoutExtension(path));
            var target = Path.Combine(Folder, name + ".lnk");

            if (File.Exists(target))
            {
                // The same shortcut was dropped before: reuse it instead of storing it twice.
                if (SameContent(path, target))
                    return target;

                for (var i = 2; File.Exists(target); i++)
                    target = Path.Combine(Folder, $"{name}-{i}.lnk");
            }

            File.Copy(path, target);
            return target;
        }
        catch (Exception ex)
        {
            Diagnostic.Record($"store {path}", ex);
            return path;
        }
    }

    private static string Sanitise(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return string.IsNullOrWhiteSpace(name) ? "shortcut" : name;
    }

    private static bool SameContent(string a, string b)
    {
        try
        {
            var x = File.ReadAllBytes(a);
            var y = File.ReadAllBytes(b);
            return x.Length == y.Length && x.AsSpan().SequenceEqual(y);
        }
        catch
        {
            return false;
        }
    }
}
