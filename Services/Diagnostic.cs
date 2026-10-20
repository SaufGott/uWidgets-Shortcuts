using System.IO;

namespace Shortcuts.Services;

/// <summary>
/// uWidgets does not catch exceptions from a widget constructor, so a failure here takes
/// down the whole application and the crash log only keeps the outer stack. This writes the
/// real exception next to the plugin so it can be read afterwards.
/// </summary>
public static class Diagnostic
{
    private static readonly object gate = new();

    public static void Record(string context, Exception ex)
    {
        try
        {
            var folder = Path.GetDirectoryName(typeof(Diagnostic).Assembly.Location) ?? Path.GetTempPath();

            lock (gate)
                File.AppendAllText(Path.Combine(folder, "Shortcuts_error.txt"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {context}\n" +
                    $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
        }
        catch
        {
            // never throw from a failure handler
        }
    }
}
