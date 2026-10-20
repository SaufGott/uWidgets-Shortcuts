using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using uWidgets.Core.Interfaces;
using Shortcuts.Models;

namespace Shortcuts.Services;

/// <summary>
/// Renders shell icons for entries, optionally tinted to the monochrome theme.
/// Results are cached across widget instances because the host re-creates the widget
/// control on every model change.
/// </summary>
public sealed class IconService
{
    private static readonly Dictionary<string, WriteableBitmap?> cache = new();
    private readonly IAppSettingsProvider settings;

    public IconService(IAppSettingsProvider settings)
    {
        this.settings = settings;
        settings.DataChanged += (_, _, _) => cache.Clear();
    }

    /// <summary>
    /// Resolve the monochrome state for this widget instance.
    /// </summary>
    public bool IsMonochromeActive(MonochromeMode mode) => mode switch
    {
        MonochromeMode.ForceOn => true,
        MonochromeMode.ForceOff => false,
        _ => settings.Get().Theme.Monochrome
    };

    /// <summary>
    /// Get the icon for an entry at the size it will be drawn at, from cache when possible.
    /// Rendering at the display size is what keeps the icons sharp: the renderer never has to
    /// resample a bitmap it did not create at that size.
    /// </summary>
    public WriteableBitmap? GetIcon(string path, MonochromeMode mode, double contrast, int size)
    {
        var monochrome = IsMonochromeActive(mode);
        var key = $"{path}|{size}|{monochrome}|{contrast:F2}";

        if (cache.TryGetValue(key, out var cached))
            return cached;

        var image = Render(path, monochrome, contrast, size);
        cache[key] = image;
        return image;
    }

    private WriteableBitmap? Render(string path, bool monochrome, double contrast, int size)
    {
        var pixels = NativeIcon.GetPixels(path, size, out var width, out var height);
        if (pixels == null)
            return null;

        if (monochrome)
            Tint(pixels, contrast);

        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96),
            PixelFormats.Bgra8888, AlphaFormat.Opaque);

        using (var buffer = bitmap.Lock())
        {
            for (var y = 0; y < height; y++)
                Marshal.Copy(pixels, y * width * 4, buffer.Address + y * buffer.RowBytes, width * 4);
        }

        return bitmap;
    }

    /// <summary>
    /// Convert every pixel to its luminance, scaled by contrast. Monochrome means grey, not
    /// the accent colour; the icon keeps its own shape and shading.
    /// </summary>
    private static void Tint(byte[] pixels, double contrast)
    {
        for (var i = 0; i < pixels.Length; i += 4)
        {
            var luminance = (pixels[i] * 114 + pixels[i + 1] * 72 + pixels[i + 2] * 19) / 255;
            var value = (byte)Math.Clamp(luminance * contrast, 0, 255);

            pixels[i] = value;
            pixels[i + 1] = value;
            pixels[i + 2] = value;
        }
    }
}
