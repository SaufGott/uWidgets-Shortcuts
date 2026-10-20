using System.Collections.ObjectModel;
using Avalonia;
using ReactiveUI;
using Shortcuts.Models;
using Shortcuts.Services;

namespace Shortcuts.ViewModels;

/// <summary>
/// One category section: header plus its own slot grid.
/// </summary>
public class CategoryViewModel : ReactiveObject
{
    public ShortcutsViewModel Root { get; }

    public Guid Id { get; }
    public string Name { get; }
    public int Columns { get; }
    public int Rows { get; }
    public int Count { get; }

    public bool ShowControls => Root.ShowControls;
    public bool CanResize => ShowControls && SlotCount > 1;

    public int SlotCount => Columns * Rows;

    /* Half the gap on each side so the space between two categories is exactly Gap. */
    public Thickness BoxMargin => new(Root.Gap / 2.0);
    public Thickness BoxPadding => new(Root.Gap / 2.0);

    /// <summary>
    /// Vertical spacing inside a category. Same as the side margins, so the title, the row and
    /// the grid all sit on the same grid.
    /// </summary>
    public double BoxSpacing => Root.Gap / 2.0;

    /// <summary>
    /// The pill needs the same inset as a slot on the top and the sides so it ends up exactly as
    /// wide as the columns. No bottom margin: the stack spacing already gives the gap below it,
    /// and a bottom margin would double it.
    /// </summary>
    public Thickness TitleMargin => new(Root.Gap / 2.0, Root.Gap / 2.0, Root.Gap / 2.0, 0);
    public int TitleFontSize => Root.TitleFontSize;

    public ObservableCollection<SlotViewModel> Slots { get; } = new();

    /// <summary>
    /// Entries that actually occupy a slot, for the editor.
    /// </summary>
    public List<EntryViewModel> Entries { get; } = new();

    public CategoryViewModel(CategoryModel model, ShortcutsViewModel root, IconService icons)
    {
        Root = root;
        Id = model.Id;
        Name = model.Name;
        Columns = model.Columns;
        Rows = model.Rows;

        var entries = root.EntriesOf(model.Id)
            .Select(e => new EntryViewModel(e, icons, root))
            .ToList();

        var slots = new SlotViewModel?[SlotCount];

        foreach (var entry in entries)
        {
            var index = entry.SlotIndex;
            if (index < 0 || index >= slots.Length || slots[index] != null)
                index = FirstFree(slots);

            if (index < 0)
                continue;

            slots[index] = new SlotViewModel(index, entry, this);
        }

        for (var i = 0; i < slots.Length; i++)
            Slots.Add(slots[i] ?? new SlotViewModel(i, null, this));

        Entries.AddRange(entries);
        Count = entries.Count;
    }

    private static int FirstFree(SlotViewModel?[] slots)
    {
        for (var i = 0; i < slots.Length; i++)
            if (slots[i] == null)
                return i;

        return -1;
    }
}
