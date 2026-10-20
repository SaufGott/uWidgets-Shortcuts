using System.Reflection;
using Shortcuts.Locales;
using Shortcuts.Models;
using Shortcuts.Views;
using Shortcuts.Views.Settings;
using uWidgets.Core.Models.Attributes;

[assembly: AssemblyCompany("uWidgets_Shortcuts")]
[assembly: AssemblyVersion("1.0.0")]

[assembly: WidgetInfo(typeof(ShortcutsView), typeof(ShortcutsModel), typeof(ShortcutsSettings),
    "Shortcuts_Title", "Shortcuts_Subtitle")]

[assembly: Locale(typeof(Locale), "Shortcuts",
    "M4,4 L11,4 L11,11 L4,11 Z M13,4 L20,4 L20,11 L13,11 Z M4,13 L11,13 L11,20 L4,20 Z M13,13 L20,13 L20,20 L13,20 Z")]
