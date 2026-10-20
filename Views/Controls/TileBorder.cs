using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Shortcuts.Locales;
using Shortcuts.ViewModels;

namespace Shortcuts.Views.Controls;

/// <summary>
/// A filled slot. Left click launches the target, right click opens the entry menu,
/// and dragging the tile moves the entry to another slot or category.
/// </summary>
public class TileBorder : Border
{
    private const double DragThreshold = 6;

    private Point? pressed;
    private bool pressing;
    private bool dragged;

    public TileBorder()
    {
        ContextFlyout = BuildMenu();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);

        if (point.Properties.IsRightButtonPressed)
        {
            if (ContextFlyout != null)
                ContextFlyout.ShowAt(this);
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
            return;

        // The host's Widget window starts a move-drag on any left press, which captures the
        // pointer and swallows the release. Handling the press here keeps a click a click.
        e.Handled = true;

        pressed = point.Position;
        pressing = true;
        dragged = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!pressing || dragged || pressed == null)
            return;

        if (DataContext is not SlotViewModel slot || slot.Entry == null || !slot.Root.ShowControls)
            return;

        if (Distance(e.GetPosition(this), pressed.Value) < DragThreshold)
            return;

        pressing = false;
        dragged = true;

        _ = DragDrop.DoDragDrop(e, CategoryHeaderBorder.Data(DragFormat.Entry, slot.Entry.Id), DragDropEffects.Move);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        pressing = false;
        pressed = null;

        if (dragged)
        {
            dragged = false;
            return;
        }

        if (e.InitialPressMouseButton != MouseButton.Left)
            return;

        if (DataContext is SlotViewModel slot && slot.Entry != null)
            slot.Root.Launch(slot.Entry);
    }

    private static double Distance(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    private MenuFlyout BuildMenu()
    {
        var flyout = new MenuFlyout();

        var open = new MenuItem { Header = Locale.Open };
        open.Click += (_, _) =>
        {
            if (DataContext is SlotViewModel slot && slot.Entry != null)
                slot.Root.Launch(slot.Entry);
        };

        var folder = new MenuItem { Header = Locale.OpenContainingFolder };
        folder.Click += (_, _) =>
        {
            if (DataContext is SlotViewModel slot && slot.Entry != null)
                slot.Root.OpenContainingFolder(slot.Entry);
        };

        var remove = new MenuItem { Header = Locale.Remove };
        remove.Click += (_, _) =>
        {
            if (DataContext is SlotViewModel slot && slot.Entry != null)
                slot.Root.RemoveEntry(slot.Entry.Id);
        };

        flyout.Items.Add(open);
        flyout.Items.Add(folder);
        flyout.Items.Add(remove);

        return flyout;
    }
}
