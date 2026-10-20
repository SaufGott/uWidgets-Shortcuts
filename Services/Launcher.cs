namespace Shortcuts.Services;

/// <summary>
/// Opens a shortcut target.
/// </summary>
public static class Launcher
{
    public static bool Launch(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void OpenContainingFolder(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory))
            return;

        Launch(directory);
    }
}
