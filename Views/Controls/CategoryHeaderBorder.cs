using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Shortcuts.ViewModels;

namespace Shortcuts.Views.Controls;

/// <summary>
/// A category header. Drag it onto another header to reorder the sections.
/// </summary>
public class CategoryHeaderBorder : Border
{
    private const double DragThreshold = 6;

    private Point? pressed;

    public CategoryHeaderBorder()
    {
        DragDrop.SetAllowDrop(this, true);

        AddHandler(DragDrop.DragOverEvent, HandleDragOver);
        AddHandler(DragDrop.DropEvent, HandleDrop);
    }

    private void HandleDragOver(object? sender, DragEventArgs e)
    {
        if (e.Source != this || DataContext is not CategoryViewModel category || !category.ShowControls)
            return;

        e.DragEffects = e.Data.Contains(DragFormat.Category)
            ? DragDropEffects.Move
            : DragDropEffects.None;
    }

    private void HandleDrop(object? sender, DragEventArgs e)
    {
        if (e.Source != this)
            return;

        if (DataContext is not CategoryViewModel target || !target.ShowControls)
            return;

        if (Guid.TryParse(e.Data.Get(DragFormat.Category) as string, out var id))
            target.Root.MoveCategory(id, target.Id);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (DataContext is not CategoryViewModel category || !category.ShowControls)
            return;

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        // Same as the tile: the host would otherwise start a move-drag and rebuild the widget
        // on release, which cancels the category drag.
        e.Handled = true;
        pressed = e.GetPosition(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (pressed == null || DataContext is not CategoryViewModel category || !category.ShowControls)
            return;

        if (Distance(e.GetPosition(this), pressed.Value) < DragThreshold)
            return;

        pressed = null;
        _ = DragDrop.DoDragDrop(e, Data(DragFormat.Category, category.Id), DragDropEffects.Move);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        pressed = null;
    }

    private static double Distance(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    internal static IDataObject Data(string format, Guid id)
    {
        var data = new DataObject();
        data.Set(format, id.ToString());
        return data;
    }
}
