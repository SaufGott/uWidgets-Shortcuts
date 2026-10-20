using System.Text.Json.Serialization;

namespace Shortcuts.Models;

/// <summary>
/// Where the category block sits inside the content area. The values are ordered as a 3x3
/// grid so the settings picker can show them the same way.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GridAlignment
{
    TopLeft,
    TopCenter,
    TopRight,
    CenterLeft,
    Center,
    CenterRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}
