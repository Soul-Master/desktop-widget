using System;
using System.Runtime.InteropServices;

namespace DesktopWidget.UI;

// The same GDI+ drawing operations previously used through System.Drawing, without
// a Windows Desktop framework dependency. Returned HICONs are owned by the caller.
internal sealed partial class TrayIconRenderer : IDisposable
{
    private UIntPtr token;

    public TrayIconRenderer()
    {
        var input = new StartupInput { Version = 1 };
        Check(GdiplusStartup(out token, ref input, IntPtr.Zero));
    }

    public IntPtr Render(int? percent, int size)
    {
        if (token == UIntPtr.Zero) throw new ObjectDisposedException(nameof(TrayIconRenderer));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        IntPtr bitmap = IntPtr.Zero, graphics = IntPtr.Zero, family = IntPtr.Zero;
        IntPtr format = IntPtr.Zero, measuringFont = IntPtr.Zero, font = IntPtr.Zero, brush = IntPtr.Zero;
        try
        {
            Check(GdipCreateBitmapFromScan0(size, size, 0, 0x26200A, IntPtr.Zero, out bitmap)); // 32bpp ARGB
            Check(GdipGetImageGraphicsContext(bitmap, out graphics));
            Check(GdipSetTextRenderingHint(graphics, 3)); // AntiAliasGridFit
            Check(GdipGraphicsClear(graphics, 0));
            Check(GdipStringFormatGetGenericTypographic(out format));
            Check(GdipGetStringFormatFlags(format, out var flags));
            Check(GdipSetStringFormatFlags(format, flags | 0x1000)); // NoWrap
            Check(GdipCreateFontFamilyFromName("Segoe UI", IntPtr.Zero, out family));
            Check(GdipCreateFont(family, size, 0, 2, out measuringFont)); // Regular, pixels
            var text = percent.HasValue ? $"{percent.Value}" : "-";
            var reference = percent == 100 ? "100" : "88";
            var measured = Measure(graphics, reference, measuringFont, format);
            var fontSize = Math.Min(size * 0.72f, size * (size - 1f) / measured.Width);
            Check(GdipCreateFont(family, fontSize, 0, 2, out font));
            var bounds = Measure(graphics, text, font, format);
            var layout = new NativeRectangle
            {
                X = (float)Math.Floor((size - bounds.Width) / 2),
                Y = (float)Math.Floor((size - bounds.Height) / 2)
            };
            Check(GdipCreateSolidFill(0xFFFFFFFF, out brush));
            Check(GdipDrawString(graphics, text, text.Length, font, ref layout, format, brush));
            Check(GdipCreateHICONFromBitmap(bitmap, out var icon));
            return icon;
        }
        finally
        {
            if (brush != IntPtr.Zero) GdipDeleteBrush(brush);
            if (font != IntPtr.Zero) GdipDeleteFont(font);
            if (measuringFont != IntPtr.Zero) GdipDeleteFont(measuringFont);
            if (family != IntPtr.Zero) GdipDeleteFontFamily(family);
            if (format != IntPtr.Zero) GdipDeleteStringFormat(format);
            if (graphics != IntPtr.Zero) GdipDeleteGraphics(graphics);
            if (bitmap != IntPtr.Zero) GdipDisposeImage(bitmap);
        }
    }

    private static NativeRectangle Measure(IntPtr graphics, string text, IntPtr font, IntPtr format)
    {
        var layout = new NativeRectangle();
        Check(GdipMeasureString(graphics, text, text.Length, font, ref layout, format, out var bounds, out _, out _));
        return bounds;
    }

    private static void Check(int status)
    {
        if (status != 0) throw new InvalidOperationException($"Tray icon rendering failed (GDI+ status {status}).");
    }

    public void Dispose()
    {
        if (token == UIntPtr.Zero) return;
        GdiplusShutdown(token);
        token = UIntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInput
    {
        public uint Version;
        public IntPtr DebugCallback;
        public int SuppressBackgroundThread, SuppressExternalCodecs;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle { public float X, Y, Width, Height; }

    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdiplusStartup(out UIntPtr token, ref StartupInput input, IntPtr output);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern void GdiplusShutdown(UIntPtr token);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipCreateBitmapFromScan0(int width, int height, int stride, int format, IntPtr scan0, out IntPtr bitmap);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipGetImageGraphicsContext(IntPtr image, out IntPtr graphics);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipSetTextRenderingHint(IntPtr graphics, int hint);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipGraphicsClear(IntPtr graphics, uint color);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipStringFormatGetGenericTypographic(out IntPtr format);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipGetStringFormatFlags(IntPtr format, out int flags);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipSetStringFormatFlags(IntPtr format, int flags);
    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)] private static extern int GdipCreateFontFamilyFromName(string name, IntPtr collection, out IntPtr family);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipCreateFont(IntPtr family, float size, int style, int unit, out IntPtr font);
    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)] private static extern int GdipMeasureString(IntPtr graphics, string text, int length, IntPtr font, ref NativeRectangle layout, IntPtr format, out NativeRectangle bounds, out int fitted, out int lines);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipCreateSolidFill(uint color, out IntPtr brush);
    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)] private static extern int GdipDrawString(IntPtr graphics, string text, int length, IntPtr font, ref NativeRectangle layout, IntPtr format, IntPtr brush);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipCreateHICONFromBitmap(IntPtr bitmap, out IntPtr icon);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipDeleteBrush(IntPtr brush);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipDeleteFont(IntPtr font);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipDeleteFontFamily(IntPtr family);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipDeleteStringFormat(IntPtr format);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipDeleteGraphics(IntPtr graphics);
    [DllImport("gdiplus.dll", ExactSpelling = true)] private static extern int GdipDisposeImage(IntPtr image);
}
