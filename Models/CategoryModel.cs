namespace Shortcuts.Models;

/// <summary>
/// A user-defined group of shortcuts. Each category renders as its own slot grid.
/// </summary>
/// <param name="Id">Stable identifier.</param>
/// <param name="Name">Category name shown as the section header.</param>
/// <param name="Columns">Items per row.</param>
/// <param name="Rows">Item slots.</param>
/// <param name="SortIndex">Order of the sections inside the widget.</param>
public record CategoryModel(Guid Id, string Name, int Columns, int Rows, int SortIndex)
{
    public const int MaxColumns = 8;
    public const int MaxRows = 12;

    /// <summary>Number of slots this category renders.</summary>
    public int SlotCount => Columns * Rows;

    public CategoryModel WithGrid(int columns, int rows) => this with
    {
        Columns = Math.Clamp(columns, 1, MaxColumns),
        Rows = Math.Clamp(rows, 1, MaxRows)
    };
}
