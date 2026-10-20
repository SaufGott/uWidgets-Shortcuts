using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia;
using ReactiveUI;
using Shortcuts.Models;
using Shortcuts.Services;
using uWidgets.Core.Interfaces;

namespace Shortcuts.ViewModels;

/// <summary>
/// Widget view model. Owns the model, the persistence and every mutation.
/// </summary>
public class ShortcutsViewModel : ReactiveObject
{
    private ShortcutsModel model;
    private readonly IWidgetLayoutProvider provider;

    public IconService Icons { get; }

    public ShortcutsViewModel(ShortcutsModel model, IWidgetLayoutProvider provider, IconService icons)
    {
        this.model = model;
        this.provider = provider;
        Icons = icons;
        Build();
    }

    public string Title => model.Title;

    /// <summary>
    /// Settable so the checkbox in the Edit window can write back through the binding.
    /// </summary>
    public bool Locked
    {
        get => model.Locked;
        set => SetLocked(value);
    }

    private bool compact;

    /// <summary>
    /// Set by the view when the widget is too small to show the inline controls.
    /// </summary>
    public bool Compact
    {
        get => compact;
        set => this.RaiseAndSetIfChanged(ref compact, value);
    }

    public bool ShowControls => !model.Locked && !compact;
    public bool MonochromeActive => Icons.IsMonochromeActive(model.MonochromeMode);
    public MonochromeMode MonochromeMode => model.MonochromeMode;

    public ObservableCollection<CategoryViewModel> Categories { get; } = new();

    public List<MonochromeMode> MonochromeOptions { get; } =
        [MonochromeMode.Inherit, MonochromeMode.ForceOn, MonochromeMode.ForceOff];

    /* ------------------------------------------------------------------ layout */

    // The layout fields are nullable so an older layout.json without them keeps the design
    // defaults instead of collapsing to zero.
    private static int Normalised(int? value, int min, int max, int fallback) =>
        value == null ? fallback : Math.Clamp(value.Value, min, max);

    public int IconSize => Normalised(model.IconSize, 16, 64, 40);
    public int IconPadding => Normalised(model.IconPadding, 0, 20, 10);
    public int Gap => Normalised(model.Gap, 0, 40, 20);
    public int EdgeMargin => Normalised(model.EdgeMargin, 0, 40, 0);
    public int TitleFontSize => Normalised(model.TitleFontSize, 8, 24, 14);
    public GridAlignment Alignment => model.Alignment;

    public Thickness EdgePadding => new(EdgeMargin);

    public void SetIconSize(int value) => ApplyLayout(Normalised(value, 16, 64, 40), IconPadding, Gap, EdgeMargin, TitleFontSize, Alignment);
    public void SetIconPadding(int value) => ApplyLayout(IconSize, Normalised(value, 0, 20, 10), Gap, EdgeMargin, TitleFontSize, Alignment);
    public void SetGap(int value) => ApplyLayout(IconSize, IconPadding, Normalised(value, 0, 40, 20), EdgeMargin, TitleFontSize, Alignment);
    public void SetEdgeMargin(int value) => ApplyLayout(IconSize, IconPadding, Gap, Normalised(value, 0, 40, 0), TitleFontSize, Alignment);
    public void SetTitleFontSize(int value) => ApplyLayout(IconSize, IconPadding, Gap, EdgeMargin, Normalised(value, 8, 24, 14), Alignment);
    public void SetAlignment(GridAlignment alignment) => ApplyLayout(IconSize, IconPadding, Gap, EdgeMargin, TitleFontSize, alignment);

    private void ApplyLayout(int iconSize, int iconPadding, int gap, int edgeMargin, int titleFontSize, GridAlignment alignment)
    {
        model = model with
        {
            IconSize = iconSize,
            IconPadding = iconPadding,
            Gap = gap,
            EdgeMargin = edgeMargin,
            TitleFontSize = titleFontSize,
            Alignment = alignment
        };

        Commit();
    }

    /* One property per cell of the 3x3 picker in the Edit window. */

    public bool AlignTopLeft { get => Is(GridAlignment.TopLeft); set => Pick(value, GridAlignment.TopLeft); }
    public bool AlignTopCenter { get => Is(GridAlignment.TopCenter); set => Pick(value, GridAlignment.TopCenter); }
    public bool AlignTopRight { get => Is(GridAlignment.TopRight); set => Pick(value, GridAlignment.TopRight); }
    public bool AlignCenterLeft { get => Is(GridAlignment.CenterLeft); set => Pick(value, GridAlignment.CenterLeft); }
    public bool AlignCenter { get => Is(GridAlignment.Center); set => Pick(value, GridAlignment.Center); }
    public bool AlignCenterRight { get => Is(GridAlignment.CenterRight); set => Pick(value, GridAlignment.CenterRight); }
    public bool AlignBottomLeft { get => Is(GridAlignment.BottomLeft); set => Pick(value, GridAlignment.BottomLeft); }
    public bool AlignBottomCenter { get => Is(GridAlignment.BottomCenter); set => Pick(value, GridAlignment.BottomCenter); }
    public bool AlignBottomRight { get => Is(GridAlignment.BottomRight); set => Pick(value, GridAlignment.BottomRight); }

    private bool Is(GridAlignment alignment) => model.Alignment == alignment;

    private void Pick(bool selected, GridAlignment alignment)
    {
        if (selected)
            SetAlignment(alignment);
    }

    public IEnumerable<ShortcutEntry> EntriesOf(Guid categoryId) => model.EntriesOf(categoryId);

    public CategoryModel? Category(Guid id) => model.Category(id);

    /* ------------------------------------------------------------------ mutations */

    public void ResizeCategory(Guid categoryId, int columns, int rows)
    {
        var category = model.Category(categoryId);
        if (category == null)
            return;

        var index = model.Categories.IndexOf(category);
        model.Categories[index] = category.WithGrid(columns, rows);
        Repack(categoryId);
        Commit();
    }

    public void MoveEntry(Guid entryId, Guid categoryId, int slotIndex)
    {
        var entry = model.Entry(entryId);
        var category = model.Category(categoryId);
        if (entry == null || category == null)
            return;

        slotIndex = Math.Clamp(slotIndex, 0, category.SlotCount - 1);

        var target = model.Entries.FirstOrDefault(e =>
            e.CategoryId == categoryId && e.SlotIndex == slotIndex && e.Id != entryId);

        var index = model.Entries.IndexOf(entry);
        model.Entries[index] = entry with { CategoryId = categoryId, SlotIndex = slotIndex };

        if (target != null)
        {
            var targetIndex = model.Entries.IndexOf(target);
            model.Entries[targetIndex] = target with { SlotIndex = entry.SlotIndex };
        }

        Commit();
    }

    public void MoveCategory(Guid draggedId, Guid targetId)
    {
        var dragged = model.Category(draggedId);
        var target = model.Category(targetId);
        if (dragged == null || target == null || dragged.Id == target.Id)
            return;

        var order = model.OrderedCategories().ToList();
        var from = order.IndexOf(dragged);
        var to = order.IndexOf(target);

        order.RemoveAt(from);
        order.Insert(to, dragged);

        for (var i = 0; i < order.Count; i++)
        {
            var index = model.Categories.IndexOf(order[i]);
            model.Categories[index] = order[i] with { SortIndex = i };
        }

        Commit();
    }

    public void AddEntry(Guid categoryId, string path, string? name)
    {
        var category = model.Category(categoryId);
        if (category == null || string.IsNullOrWhiteSpace(path))
            return;

        var slot = FirstFreeSlot(category);
        if (slot < 0)
            return;

        // Store the shortcut in the widget's own folder so the entry survives deleting the
        // original file on the desktop.
        var stored = ShortcutStore.Store(path.Trim());
        var label = string.IsNullOrWhiteSpace(name) ? DefaultLabel(stored) : name.Trim();

        model.Entries.Add(new ShortcutEntry(Guid.NewGuid(), categoryId, slot, label,
            stored, null, ShortcutEntry.DefaultContrast));

        Commit();
    }

    public void RemoveEntry(Guid entryId)
    {
        var entry = model.Entry(entryId);
        if (entry == null)
            return;

        model.Entries.Remove(entry);
        Commit();
    }

    public void RemoveCategory(Guid categoryId)
    {
        var category = model.Category(categoryId);
        if (category == null)
            return;

        model.Categories.Remove(category);
        model.Entries.RemoveAll(e => e.CategoryId == categoryId);
        Commit();
    }

    public void SetLocked(bool locked)
    {
        if (model.Locked == locked)
            return;

        model = model with { Locked = locked };

        this.RaisePropertyChanged(nameof(Locked));
        this.RaisePropertyChanged(nameof(ShowControls));

        Commit();
    }

    public void AddCategory(string name)
    {
        var category = new CategoryModel(Guid.NewGuid(),
            string.IsNullOrWhiteSpace(name) ? "Category" : name.Trim(), 3, 2, model.Categories.Count);

        model.Categories.Add(category);
        Commit();
    }

    public void RenameCategory(Guid categoryId, string name)
    {
        var category = model.Category(categoryId);
        if (category == null || string.IsNullOrWhiteSpace(name))
            return;

        var index = model.Categories.IndexOf(category);
        model.Categories[index] = category with { Name = name.Trim() };
        Commit();
    }

    public void RenameEntry(Guid entryId, string name)
    {
        var entry = model.Entry(entryId);
        if (entry == null)
            return;

        var index = model.Entries.IndexOf(entry);
        model.Entries[index] = entry with { Name = string.IsNullOrWhiteSpace(name) ? DefaultLabel(entry.TargetPath) : name.Trim() };
        Commit();
    }

    public void SetEntryPath(Guid entryId, string path)
    {
        var entry = model.Entry(entryId);
        if (entry == null || string.IsNullOrWhiteSpace(path))
            return;

        var index = model.Entries.IndexOf(entry);
        model.Entries[index] = entry with { TargetPath = path.Trim() };
        Commit();
    }

    public void SetEntryIcon(Guid entryId, string? iconPath)
    {
        var entry = model.Entry(entryId);
        if (entry == null)
            return;

        var index = model.Entries.IndexOf(entry);
        model.Entries[index] = entry with { IconPath = string.IsNullOrWhiteSpace(iconPath) ? null : iconPath.Trim() };
        Commit();
    }

    public void SetContrast(Guid entryId, double contrast, bool commit = true)
    {
        var entry = model.Entry(entryId);
        if (entry == null)
            return;

        var index = model.Entries.IndexOf(entry);
        model.Entries[index] = entry.WithContrast(contrast);

        if (commit)
            Commit();
    }

    public void Launch(EntryViewModel entry) => Launcher.Launch(entry.TargetPath);

    public void OpenContainingFolder(EntryViewModel entry)
    {
        var target = entry.ResolvedTarget ?? entry.TargetPath;
        Launcher.OpenContainingFolder(target);
    }

    /* ------------------------------------------------------------------ internals */

    public void Commit()
    {
        Save();
        Build();
    }

    private void Save() =>
        provider.Save(provider.Get() with { Settings = JsonSerializer.SerializeToElement(model) });

    private void Replace(ShortcutsModel clone) => model = clone;

    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title == model.Title)
            return;

        Replace(model with { Title = title.Trim() });
        Commit();
    }

    public void SetMonochromeMode(MonochromeMode mode)
    {
        if (mode == model.MonochromeMode)
            return;

        Replace(model with { MonochromeMode = mode });
        Commit();
    }

    private void Repack(Guid categoryId)
    {
        var category = model.Category(categoryId);
        if (category == null)
            return;

        var entries = model.EntriesOf(categoryId).ToList();
        var used = new HashSet<int>();

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var index = model.Entries.IndexOf(entry);

            if (entry.SlotIndex >= category.SlotCount || used.Contains(entry.SlotIndex))
            {
                var free = -1;
                for (var s = 0; s < category.SlotCount; s++)
                    if (!used.Contains(s))
                    {
                        free = s;
                        break;
                    }

                if (free < 0)
                    continue;

                used.Add(free);
                model.Entries[index] = entry with { SlotIndex = free };
            }
            else
            {
                used.Add(entry.SlotIndex);
            }
        }
    }

    private int FirstFreeSlot(CategoryModel category)
    {
        var used = model.EntriesOf(category.Id).Select(e => e.SlotIndex).ToHashSet();
        for (var i = 0; i < category.SlotCount; i++)
            if (!used.Contains(i))
                return i;

        return -1;
    }

    private static string DefaultLabel(string path)
    {
        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(name))
            return path;

        var extension = Path.GetExtension(name);
        return extension.Length > 0 ? name[..(name.Length - extension.Length)] : name;
    }

    private void Build()
    {
        Categories.Clear();
        foreach (var category in model.OrderedCategories())
        {
            try
            {
                Categories.Add(new CategoryViewModel(category, this, Icons));
            }
            catch (Exception ex)
            {
                Diagnostic.Record($"category {category.Name}", ex);
            }
        }
    }
}
