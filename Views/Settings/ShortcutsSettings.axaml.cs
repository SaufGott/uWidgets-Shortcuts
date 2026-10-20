using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Shortcuts.Models;
using Shortcuts.Services;
using Shortcuts.ViewModels;
using uWidgets.Core.Interfaces;

namespace Shortcuts.Views.Settings;

/// <summary>
/// The "Edit Widget" window: full editor for the model.
/// </summary>
public partial class ShortcutsSettings : UserControl
{
    private readonly ShortcutsViewModel viewModel;
    private readonly DispatcherTimer debounce = new() { Interval = TimeSpan.FromMilliseconds(400) };

    public ShortcutsSettings(IWidgetLayoutProvider provider, IAppSettingsProvider settings)
    {
        var model = provider.Get().GetModel<ShortcutsModel>() ?? ShortcutsModel.Default();
        viewModel = new ShortcutsViewModel(model, provider, new IconService(settings));
        DataContext = viewModel;

        debounce.Tick += (_, _) =>
        {
            debounce.Stop();
            viewModel.Commit();
        };

        InitializeComponent();
    }

    /* ------------------------------------------------------------------ widget */

    private void UpdateTitle(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
            viewModel.SetTitle(textBox.Text ?? string.Empty);
    }

    private void MonochromeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedItem: MonochromeMode mode })
            viewModel.SetMonochromeMode(mode);
    }

    /* ------------------------------------------------------------------ layout */

    private void UpdateLayout(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox { Tag: string key } textBox)
            return;

        if (!int.TryParse(textBox.Text, out var value))
            return;

        switch (key)
        {
            case "IconSize": viewModel.SetIconSize(value); break;
            case "IconPadding": viewModel.SetIconPadding(value); break;
            case "Gap": viewModel.SetGap(value); break;
            case "EdgeMargin": viewModel.SetEdgeMargin(value); break;
            case "TitleFontSize": viewModel.SetTitleFontSize(value); break;
        }
    }

    /* ------------------------------------------------------------------ categories */

    private void AddCategoryKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter && e.Key != Key.Return)
            return;

        AddCategory(sender);
        e.Handled = true;
    }

    private void AddCategoryClick(object? sender, RoutedEventArgs e) => AddCategory(sender);

    private void AddCategory(object? sender)
    {
        if (sender is not TextBox textBox)
            return;

        viewModel.AddCategory(textBox.Text ?? string.Empty);
        textBox.Clear();
    }

    private void UpdateCategoryName(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: CategoryViewModel category })
            viewModel.RenameCategory(category.Id, category.Name);
    }

    private void DeleteCategory(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: CategoryViewModel category })
            viewModel.RemoveCategory(category.Id);
    }

    private void IncRows(object? sender, RoutedEventArgs e) => Resize(sender, 0, 1);
    private void DecRows(object? sender, RoutedEventArgs e) => Resize(sender, 0, -1);
    private void IncColumns(object? sender, RoutedEventArgs e) => Resize(sender, 1, 0);
    private void DecColumns(object? sender, RoutedEventArgs e) => Resize(sender, -1, 0);

    private void Resize(object? sender, int columns, int rows)
    {
        if (sender is not Control { DataContext: CategoryViewModel category })
            return;

        viewModel.ResizeCategory(category.Id, category.Columns + columns, category.Rows + rows);
    }

    /* ------------------------------------------------------------------ entries */

    private void UpdateEntryName(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: EntryViewModel entry })
            viewModel.RenameEntry(entry.Id, entry.Name);
    }

    private void UpdateEntryPath(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: EntryViewModel entry })
            viewModel.SetEntryPath(entry.Id, entry.TargetPath);
    }

    private void UpdateContrast(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider { DataContext: EntryViewModel entry } slider)
            return;

        // Writing layout.json on every tick of a drag would hammer the disk.
        viewModel.SetContrast(entry.Id, slider.Value, commit: false);
        debounce.Start();
    }

    private void DeleteEntry(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: EntryViewModel entry })
            viewModel.RemoveEntry(entry.Id);
    }
}
