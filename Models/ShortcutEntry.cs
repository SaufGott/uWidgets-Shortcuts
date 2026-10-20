namespace Shortcuts.Models;

/// <summary>
/// A single shortcut entry. <c>TargetPath</c> is either a <c>.lnk</c> file or a direct path.
/// </summary>
/// <param name="Id">Stable identifier.</param>
/// <param name="CategoryId">Owning category.</param>
/// <param name="SlotIndex">Slot inside the category grid.</param>
/// <param name="Name">Label shown in the widget.</param>
/// <param name="TargetPath">Path of the shortcut or of the target itself.</param>
/// <param name="IconPath">Optional explicit <c>.ico</c>/<c>.png</c> path; otherwise the shell icon is used.</param>
/// <param name="Contrast">Monochrome contrast factor, applied when monochrome is active.</param>
public record ShortcutEntry(Guid Id, Guid CategoryId, int SlotIndex, string Name,
    string TargetPath, string? IconPath, double Contrast)
{
    public const double MinContrast = 0.2;
    public const double MaxContrast = 2.0;
    public const double DefaultContrast = 1.0;

    public ShortcutEntry WithContrast(double contrast) => this with
    {
        Contrast = Math.Clamp(contrast, MinContrast, MaxContrast)
    };
}
