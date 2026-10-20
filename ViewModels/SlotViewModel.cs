using Avalonia;
using ReactiveUI;

namespace Shortcuts.ViewModels;

/// <summary>
/// A single slot in a category grid. Empty slots are drop targets.
/// </summary>
public class SlotViewModel : ReactiveObject
{
    public CategoryViewModel Category { get; }
    public ShortcutsViewModel Root => Category.Root;

    public int Index { get; }
    public EntryViewModel? Entry { get; }

    public bool HasEntry => Entry != null;

    /// <summary>
    /// The template box is the layout preview, so it stays after locking.
    /// The plus is the "drop here" affordance and only belongs to edit mode.
    /// </summary>
    public bool ShowBox => !HasEntry;

    public bool ShowPlus => !HasEntry && Root.ShowControls;

    /* The box the icon lives in, and the inset inside it. Half the gap on each side so the
       space between two boxes is exactly Gap. */
    public double BoxSize => Root.IconSize + 2 * Root.IconPadding;
    public double IconSize => Root.IconSize;
    public Thickness BoxMargin => new(Root.Gap / 2.0);
    public Thickness BoxPadding => new(Root.IconPadding);

    public SlotViewModel(int index, EntryViewModel? entry, CategoryViewModel category)
    {
        Index = index;
        Entry = entry;
        Category = category;
    }
}
