using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Shortcuts.Models;

namespace Shortcuts.Views.Controls;

/// <summary>
/// Lays categories out in rows. Two differences from <c>WrapPanel</c>: every item keeps its
/// own height instead of being stretched to the tallest one in the row, and the block can be
/// placed anywhere in the area it is given — the 3x3 alignment picker in the Edit window.
/// </summary>
public class CategoryPanel : Panel
{
    public static readonly StyledProperty<GridAlignment> AlignmentProperty =
        AvaloniaProperty.Register<CategoryPanel, GridAlignment>(nameof(Alignment), GridAlignment.Center);

    public GridAlignment Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    private readonly List<List<int>> rows = new();
    private Size content;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == AlignmentProperty)
            InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size constraint)
    {
        rows.Clear();

        var widths = new double[Children.Count];
        var heights = new double[Children.Count];

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.Measure(new Size(constraint.Width, double.PositiveInfinity));
            widths[i] = child.DesiredSize.Width;
            heights[i] = child.DesiredSize.Height;
        }

        var limit = double.IsPositiveInfinity(constraint.Width) ? double.PositiveInfinity : constraint.Width;
        var row = new List<int>();
        double rowWidth = 0;

        for (var i = 0; i < Children.Count; i++)
        {
            if (row.Count > 0 && rowWidth + widths[i] > limit)
            {
                rows.Add(row);
                row = new List<int>();
                rowWidth = 0;
            }

            row.Add(i);
            rowWidth += widths[i];
        }

        if (row.Count > 0)
            rows.Add(row);

        double widest = 0;
        double tallest = 0;

        foreach (var r in rows)
        {
            widest = Math.Max(widest, r.Sum(i => widths[i]));
            tallest += r.Max(i => heights[i]);
        }

        content = new Size(widest, tallest);

        return new Size(
            double.IsPositiveInfinity(constraint.Width) ? widest : constraint.Width,
            double.IsPositiveInfinity(constraint.Height) ? tallest : constraint.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var alignment = Alignment;
        var vertical = RowOf(alignment);

        var y = vertical switch
        {
            0 => 0.0,
            2 => Math.Max(0, finalSize.Height - content.Height),
            _ => Math.Max(0, (finalSize.Height - content.Height) / 2)
        };

        foreach (var row in rows)
        {
            var rowHeight = row.Max(i => Children[i].DesiredSize.Height);
            var rowWidth = row.Sum(i => Children[i].DesiredSize.Width);

            var x = RowOfHorizontal(alignment) switch
            {
                0 => 0.0,
                2 => Math.Max(0, finalSize.Width - rowWidth),
                _ => Math.Max(0, (finalSize.Width - rowWidth) / 2)
            };

            foreach (var i in row)
            {
                var size = Children[i].DesiredSize;
                Children[i].Arrange(new Rect(x, y, size.Width, size.Height));
                x += size.Width;
            }

            y += rowHeight;
        }

        return finalSize;
    }

    private static int RowOf(GridAlignment alignment) => alignment switch
    {
        GridAlignment.TopLeft or GridAlignment.TopCenter or GridAlignment.TopRight => 0,
        GridAlignment.BottomLeft or GridAlignment.BottomCenter or GridAlignment.BottomRight => 2,
        _ => 1
    };

    private static int RowOfHorizontal(GridAlignment alignment) => alignment switch
    {
        GridAlignment.TopLeft or GridAlignment.CenterLeft or GridAlignment.BottomLeft => 0,
        GridAlignment.TopRight or GridAlignment.CenterRight or GridAlignment.BottomRight => 2,
        _ => 1
    };
}
