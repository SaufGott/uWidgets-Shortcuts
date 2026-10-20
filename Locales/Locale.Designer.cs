#nullable enable

using System.Reflection;
using System.Resources;

namespace Shortcuts.Locales;

/// <summary>
/// Lookup class for the widget's strings. Kept hand-written so the members stay
/// public for <c>{x:Static}</c> bindings; the host resolves the display name through
/// the static <see cref="ResourceManager"/> property.
/// </summary>
public class Locale
{
    private static ResourceManager? resourceManager;

    public static ResourceManager ResourceManager =>
        resourceManager ??= new ResourceManager("Shortcuts.Locales.Locale", typeof(Locale).Assembly);

    public static string Shortcuts => Get(nameof(Shortcuts));
    public static string Shortcuts_Title => Get(nameof(Shortcuts_Title));
    public static string Shortcuts_Subtitle => Get(nameof(Shortcuts_Subtitle));
    public static string Open => Get(nameof(Open));
    public static string OpenContainingFolder => Get(nameof(OpenContainingFolder));
    public static string Remove => Get(nameof(Remove));
    public static string Lock => Get(nameof(Lock));
    public static string Unlock => Get(nameof(Unlock));
    public static string Add => Get(nameof(Add));
    public static string NewCategory => Get(nameof(NewCategory));
    public static string Delete => Get(nameof(Delete));
    public static string Name => Get(nameof(Name));
    public static string Path => Get(nameof(Path));
    public static string Icon => Get(nameof(Icon));
    public static string Contrast => Get(nameof(Contrast));
    public static string Monochrome => Get(nameof(Monochrome));
    public static string Inherit => Get(nameof(Inherit));
    public static string ForceOn => Get(nameof(ForceOn));
    public static string ForceOff => Get(nameof(ForceOff));
    public static string Rows => Get(nameof(Rows));
    public static string Columns => Get(nameof(Columns));
    public static string EmptySlot => Get(nameof(EmptySlot));
    public static string Title => Get(nameof(Title));
    public static string IconSize => Get(nameof(IconSize));
    public static string Gap => Get(nameof(Gap));
    public static string EdgeMargin => Get(nameof(EdgeMargin));
    public static string IconPadding => Get(nameof(IconPadding));
    public static string FontSize => Get(nameof(FontSize));
    public static string Alignment => Get(nameof(Alignment));

    private static string Get(string key) => ResourceManager.GetString(key) ?? key;
}
