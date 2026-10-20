using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace Shortcuts.Views.Controls;

/// <summary>
/// Custom drag formats used between the widget's own slots.
/// The value is the entry or category id.
/// </summary>
public static class DragFormat
{
    public const string Entry = "uwidgets-shortcut-entry";
    public const string Category = "uwidgets-shortcut-category";

    /// <summary>
    /// File paths carried by a drop from Explorer, if any.
    /// The Win32 drag source returns IStorageItem for DataFormats.Files, not strings,
    /// so both shapes have to be handled.
    /// </summary>
    public static string[] Paths(IDataObject data)
    {
        if (data.Contains(DataFormats.Files))
        {
            switch (data.Get(DataFormats.Files))
            {
                case string[] paths:
                    return paths;
                case IEnumerable<IStorageItem> items:
                    var resolved = new List<string>();
                    foreach (var item in items)
                    {
                        // IStorageItem.Path is a Uri, not a string.
                        var path = item.Path?.LocalPath;
                        if (string.IsNullOrEmpty(path))
                            path = item.Path?.ToString();
                        if (!string.IsNullOrEmpty(path))
                            resolved.Add(path!);
                    }
                    return [.. resolved];
                case IEnumerable<string> paths:
                    return [.. paths];
            }
        }

#pragma warning disable CS0618 // obsolete, but some sources still expose it
        if (data.Contains(DataFormats.FileNames) && data.Get(DataFormats.FileNames) is string[] names)
            return names;
#pragma warning restore CS0618

        return [];
    }
}
