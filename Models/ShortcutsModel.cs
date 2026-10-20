namespace Shortcuts.Models;

/// <summary>
/// Widget model, stored as <c>Settings</c> inside <c>layout.json</c>.
/// </summary>
public record ShortcutsModel(
    string Title,
    List<CategoryModel> Categories,
    List<ShortcutEntry> Entries,
    bool Locked,
    MonochromeMode MonochromeMode,
    int IconSize = 40,
    int IconPadding = 10,
    int Gap = 20,
    int EdgeMargin = 0,
    int TitleFontSize = 14,
    GridAlignment Alignment = GridAlignment.Center)
{
    public static ShortcutsModel Default() =>
        new("Shortcuts", [], [], false, MonochromeMode.Inherit);

    public IEnumerable<CategoryModel> OrderedCategories() =>
        Categories.OrderBy(c => c.SortIndex);

    public CategoryModel? Category(Guid id) => Categories.FirstOrDefault(c => c.Id == id);

    public ShortcutEntry? Entry(Guid id) => Entries.FirstOrDefault(e => e.Id == id);

    public IEnumerable<ShortcutEntry> EntriesOf(Guid categoryId) =>
        Entries.Where(e => e.CategoryId == categoryId).OrderBy(e => e.SlotIndex);
}
