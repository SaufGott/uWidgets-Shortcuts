using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI;
using Shortcuts.Models;
using Shortcuts.Services;

namespace Shortcuts.ViewModels;

/// <summary>
/// A shortcut entry as rendered inside a slot.
/// </summary>
public class EntryViewModel : ReactiveObject
{
    public ShortcutsViewModel Root { get; }

    public Guid Id { get; }
    public int SlotIndex { get; }
    public string Name { get; }
    public string TargetPath { get; }
    public string? ResolvedTarget { get; }
    public bool Exists { get; }
    public double Contrast { get; }
    public Geometry Glyph { get; }
    public WriteableBitmap? Icon { get; }

    public bool HasBitmap => Icon != null;
    public bool HasGlyph => Icon == null;

    public EntryViewModel(ShortcutEntry entry, IconService icons, ShortcutsViewModel root)
    {
        Root = root;
        Id = entry.Id;
        SlotIndex = entry.SlotIndex;
        Name = entry.Name;
        TargetPath = entry.TargetPath;
        Contrast = entry.Contrast;

        try
        {
            ResolvedTarget = LnkResolver.IsShortcut(TargetPath)
                ? LnkResolver.Resolve(TargetPath)
                : TargetPath;
        }
        catch (Exception ex)
        {
            Diagnostic.Record($"resolve {entry.Name}", ex);
        }

        Exists = ResolvedTarget != null &&
                 (File.Exists(ResolvedTarget) || Directory.Exists(ResolvedTarget));

        var iconSource = string.IsNullOrWhiteSpace(entry.IconPath) ? TargetPath : entry.IconPath;

        try
        {
            Icon = icons.GetIcon(iconSource, root.MonochromeMode, entry.Contrast, root.IconSize);
        }
        catch (Exception ex)
        {
            Diagnostic.Record($"icon {entry.Name}", ex);
        }

        try
        {
            Glyph = Geometry.Parse(Glyphs.For(ResolvedTarget ?? TargetPath));
        }
        catch (Exception ex)
        {
            Diagnostic.Record($"glyph {entry.Name}", ex);
            Glyph = new PathGeometry();
        }
    }
}
