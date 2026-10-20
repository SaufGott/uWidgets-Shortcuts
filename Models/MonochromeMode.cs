using System.Text.Json.Serialization;

namespace Shortcuts.Models;

/// <summary>
/// How this widget instance treats the monochrome theme.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MonochromeMode
{
    /// <summary>Follow the global uWidgets monochrome setting.</summary>
    Inherit,

    /// <summary>Always render icons monochrome, regardless of the global setting.</summary>
    ForceOn,

    /// <summary>Always render original icons, regardless of the global setting.</summary>
    ForceOff
}
