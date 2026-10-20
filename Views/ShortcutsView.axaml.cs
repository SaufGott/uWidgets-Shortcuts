using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Shortcuts.Models;
using Shortcuts.Services;
using Shortcuts.ViewModels;
using uWidgets.Core.Interfaces;

namespace Shortcuts.Views;

public partial class ShortcutsView : UserControl
{
    private const double CompactWidth = 140;

    private readonly ShortcutsViewModel viewModel;

    public ShortcutsView(IWidgetLayoutProvider provider, IAppSettingsProvider settings)
        : this(ShortcutsModel.Default(), provider, settings)
    {
    }

    public ShortcutsView(ShortcutsModel model, IWidgetLayoutProvider provider, IAppSettingsProvider settings)
    {
        viewModel = new ShortcutsViewModel(model, provider, new IconService(settings));
        DataContext = viewModel;

        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Diagnostic.Record("view", ex);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != BoundsProperty)
            return;

        var compact = change.GetNewValue<Rect>().Width < CompactWidth;
        if (compact != viewModel.Compact)
            viewModel.Compact = compact;
    }

    private void IncRows(object? sender, RoutedEventArgs e) => Resize(sender, 0, 1);
    private void DecRows(object? sender, RoutedEventArgs e) => Resize(sender, 0, -1);
    private void IncColumns(object? sender, RoutedEventArgs e) => Resize(sender, 1, 0);
    private void DecColumns(object? sender, RoutedEventArgs e) => Resize(sender, -1, 0);

    private void Resize(object? sender, int columns, int rows)
    {
        if (sender is not Control control || control.DataContext is not CategoryViewModel category)
            return;

        viewModel.ResizeCategory(category.Id, category.Columns + columns, category.Rows + rows);
    }
}
