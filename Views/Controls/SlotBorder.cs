using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Shortcuts.ViewModels;

namespace Shortcuts.Views.Controls;

/// <summary>
/// A slot in the category grid. Accepts entries dragged from another slot and
/// files dragged in from Explorer.
/// </summary>
public class SlotBorder : Border
{
    public SlotBorder()
    {
        DragDrop.SetAllowDrop(this, true);

        AddHandler(DragDrop.DragOverEvent, HandleDragOver);
        AddHandler(DragDrop.DragLeaveEvent, HandleDragLeave);
        AddHandler(DragDrop.DropEvent, HandleDrop);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (DataContext is not SlotViewModel slot || !slot.Root.ShowControls)
            return;

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        // Stop the host from starting a move-drag: it saves the position on release, which
        // rebuilds the widget and cancels the drop that is in progress.
        e.Handled = true;
    }

    private void HandleDragOver(object? sender, DragEventArgs e)
    {
        if (DataContext is not SlotViewModel slot || !slot.Root.ShowControls)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var accepted = e.Data.Contains(DragFormat.Entry) || DragFormat.Paths(e.Data).Length > 0;

        e.DragEffects = accepted ? DragDropEffects.Copy : DragDropEffects.None;

        if (accepted)
            Classes.Add("dragover");
        else
            Classes.Remove("dragover");
    }

    private void HandleDragLeave(object? sender, DragEventArgs e) => Classes.Remove("dragover");

    private void HandleDrop(object? sender, DragEventArgs e)
    {
        Classes.Remove("dragover");

        if (DataContext is not SlotViewModel slot || !slot.Root.ShowControls)
            return;

        if (e.Data.Contains(DragFormat.Entry))
        {
            if (Guid.TryParse(e.Data.Get(DragFormat.Entry) as string, out var id))
                slot.Root.MoveEntry(id, slot.Category.Id, slot.Index);
            return;
        }

        var paths = DragFormat.Paths(e.Data);
        if (paths.Length > 0)
            slot.Root.AddEntry(slot.Category.Id, paths[0], null);
    }
}
