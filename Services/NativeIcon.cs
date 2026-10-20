using System.Runtime.InteropServices;

namespace Shortcuts.Services;

/// <summary>
/// Shell icon extraction through Win32 only: the file's own icon resource at the requested
/// size, falling back to SHGetFileInfo. No System.Drawing, no COM, no extra packages.
/// </summary>
public static class NativeIcon
{
    private const uint SHGFI_ICON = 0x00000100;
    private const uint SHGFI_LARGEICON = 0x00000000;
    private const uint SHGFI_SMALLICON = 0x00000001;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x00000010;
    private const uint DIB_RGB_COLORS = 0;
    private const uint LR_DEFAULTCOLOR = 0x0000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShellFileInfo
    {
        public IntPtr hIcon;
        public IntPtr iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szTypeName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IconInfo
    {
        public bool fIcon;
        public uint xHotspot;
        public uint yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader bmiHeader;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Bitmap
    {
        public int bmType;
        public int bmWidth;
        public int bmHeight;
        public int bmWidthBytes;
        public int bmPlanes;
        public int bmBitsPixel;
        public IntPtr bmBits;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
        ref ShellFileInfo psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint PrivateExtractIconsW(string filename, int index, int width, int height,
        IntPtr[] phicon, uint[] pResID, uint count, uint flags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void ReleaseDC(IntPtr hWnd, IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern int GetIconInfo(IntPtr hIcon, out IconInfo piconinfo);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint cLines,
        byte[] lpvBits, ref BitmapInfo lpbmi, uint usage);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr ho);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetObjectW(IntPtr h, int n, ref Bitmap bm);

    /// <summary>
    /// Extract the icon as BGRA pixels, top-down. Returns <c>null</c> when there is no icon
    /// for the path.
    /// </summary>
    public static byte[]? GetPixels(string path, int size, out int width, out int height)
    {
        width = 0;
        height = 0;

        // SHGetFileInfo is capped at 32 px, which looks soft when drawn larger. The file's own
        // icon resource can be asked for the size we want, so try that first. For a shortcut
        // that means its target, since a .lnk has no icon resource of its own.
        foreach (var candidate in Candidates(path))
        {
            if (string.IsNullOrEmpty(candidate) || !File.Exists(candidate))
                continue;

            var handles = new IntPtr[1];
            if (PrivateExtractIconsW(candidate, 0, size, size, handles, new uint[1], 1, LR_DEFAULTCOLOR) == 0)
                continue;

            if (handles[0] == IntPtr.Zero)
                continue;

            try
            {
                var pixels = ReadIcon(handles[0], out width, out height);
                if (pixels != null)
                    return pixels;
            }
            finally
            {
                DestroyIcon(handles[0]);
            }
        }

        var info = new ShellFileInfo();
        var flags = SHGFI_ICON | (size >= 32 ? SHGFI_LARGEICON : SHGFI_SMALLICON);
        if (!File.Exists(path))
            flags |= SHGFI_USEFILEATTRIBUTES;

        SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf<ShellFileInfo>(), flags);
        if (info.hIcon == IntPtr.Zero)
            return null;

        try
        {
            return ReadIcon(info.hIcon, out width, out height);
        }
        finally
        {
            DestroyIcon(info.hIcon);
        }
    }

    private static IEnumerable<string> Candidates(string path)
    {
        if (LnkResolver.IsShortcut(path))
            return [path, LnkResolver.Resolve(path) ?? path];

        return [path];
    }

    private static byte[]? ReadIcon(IntPtr hIcon, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (GetIconInfo(hIcon, out var icon) == 0)
            return null;

        var hdc = GetDC(IntPtr.Zero);
        try
        {
            // Monochrome icons have no colour bitmap: the mask is the image itself.
            var source = icon.hbmColor != IntPtr.Zero ? icon.hbmColor : icon.hbmMask;
            if (source == IntPtr.Zero)
                return null;

            // GetIconInfo reports the hotspot, which for an icon created at a custom size is
            // half the icon rather than its dimensions. Read the bitmap itself instead.
            var bm = new Bitmap();
            if (GetObjectW(source, Marshal.SizeOf<Bitmap>(), ref bm) == 0)
                return null;

            var w = bm.bmWidth;
            var h = bm.bmHeight;
            if (w <= 0 || h <= 0)
                return null;

            var bitmapInfo = new BitmapInfo();
            bitmapInfo.bmiHeader.biSize = (uint)Marshal.SizeOf<BitmapInfoHeader>();
            bitmapInfo.bmiHeader.biWidth = w;
            bitmapInfo.bmiHeader.biHeight = -h; // top-down
            bitmapInfo.bmiHeader.biPlanes = 1;
            bitmapInfo.bmiHeader.biBitCount = 32;

            var pixels = new byte[w * h * 4];
            if (GetDIBits(hdc, source, 0, (uint)h, pixels, ref bitmapInfo, DIB_RGB_COLORS) == 0)
                return null;

            if (icon.hbmColor != IntPtr.Zero && icon.hbmMask != IntPtr.Zero && !HasAlpha(pixels))
                ApplyAndMask(hdc, icon.hbmMask, pixels, w, h);

            width = w;
            height = h;
            return pixels;
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdc);
            if (icon.hbmColor != IntPtr.Zero) DeleteObject(icon.hbmColor);
            if (icon.hbmMask != IntPtr.Zero) DeleteObject(icon.hbmMask);
        }
    }

    private static bool HasAlpha(byte[] pixels)
    {
        for (var i = 3; i < pixels.Length; i += 4)
            if (pixels[i] != 0)
                return true;

        return false;
    }

    /// <summary>
    /// Rebuild alpha from the AND mask: a set bit means the pixel is background.
    /// </summary>
    private static void ApplyAndMask(IntPtr hdc, IntPtr mask, byte[] pixels, int width, int height)
    {
        var rowBytes = ((width + 31) / 32) * 4;
        var buffer = new byte[rowBytes * height];

        var bitmapInfo = new BitmapInfo();
        bitmapInfo.bmiHeader.biSize = (uint)Marshal.SizeOf<BitmapInfoHeader>();
        bitmapInfo.bmiHeader.biWidth = width;
        bitmapInfo.bmiHeader.biHeight = -height;
        bitmapInfo.bmiHeader.biPlanes = 1;
        bitmapInfo.bmiHeader.biBitCount = 1;

        if (GetDIBits(hdc, mask, 0, (uint)height, buffer, ref bitmapInfo, DIB_RGB_COLORS) == 0)
            return;

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var bit = (buffer[y * rowBytes + x / 8] >> (7 - (x % 8))) & 1;
            pixels[(y * width + x) * 4 + 3] = (byte)(bit == 1 ? 0 : 255);
        }
    }
}
