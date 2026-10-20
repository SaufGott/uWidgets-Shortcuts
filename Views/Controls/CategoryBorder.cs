using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Shortcuts.ViewModels;

namespace Shortcuts.Views.Controls;

/// <summary>
/// A category box. While the widget is unlocked it can be grabbed anywhere on the box and
/// dropped onto another category to reorder them.
/// </summary>
public class CategoryBorder : Border
{
    private const double DragThreshold = 6;

    private Point? pressed;

    public CategoryBorder()
    {
        DragDrop.SetAllowDrop(this, true);

        AddHandler(DragDrop.DragOverEvent, HandleDragOver);
        AddHandler(DragDrop.DropEvent, HandleDrop);
    }

    private void HandleDragOver(object? sender, DragEventArgs e)
    {
        // A drop that landed on a slot inside this box was already decided by that slot.
        if (e.Source != this || DataContext is not CategoryViewModel category || !category.ShowControls)
        {
            if (e.Source == this)
                e.DragEffects = DragDropEffects.None;

            return;
        }

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

        // The host starts a move-drag on any left press that is not handled, and saves the
        // position on release — that rebuilds the whole widget and cancels the drag.
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
        _ = DragDrop.DoDragDrop(e, CategoryHeaderBorder.Data(DragFormat.Category, category.Id), DragDropEffects.Move);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        pressed = null;
    }

    private static double Distance(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
