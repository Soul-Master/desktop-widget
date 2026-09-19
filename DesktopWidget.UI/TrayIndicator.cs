using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;
namespace DesktopWidget.UI;
internal sealed class TrayIndicator : IDisposable
{
    private readonly Forms.NotifyIcon icon;
    private string? previous;
    private int previousSize;
    private string? previousTooltip;
    private int? previousPercent;
    public TrayIndicator(Action toggle, Action refresh, Action exit)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show / hide widget", null, (_, _) => toggle());
        menu.Items.Add("Refresh limits", null, (_, _) => refresh());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => exit());
        icon = new Forms.NotifyIcon { ContextMenuStrip = menu, Text = "Codex - Usage Limits" };
        icon.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) toggle(); };
        Update(null, "Codex - Usage Limits\nConnecting…");
        icon.Visible = true;
    }
    public void Update(int? percent, string tooltip)
    {
        if (tooltip != previousTooltip)
        {
            icon.Text = tooltip.Length > 127 ? tooltip[..127] : tooltip;
            previousTooltip = tooltip;
        }
        // Rasterize at the taskbar's actual small-icon size instead of letting
        // Explorer shrink a 64px vector drawing (which blurs the thin strokes).
        var taskbar = FindWindow("Shell_TrayWnd", null);
        var dpi = taskbar != IntPtr.Zero ? GetDpiForWindow(taskbar) : 96;
        var size = Math.Max(16, GetSystemMetricsForDpi(49, dpi == 0 ? 96 : dpi));
        if (previous is not null && percent == previousPercent && size == previousSize) return;
        var text = percent.HasValue ? $"{percent.Value}" : "-";
        previousPercent = percent;
        previous = text;
        previousSize = size;
        using var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.Clear(Color.Transparent);
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        format.FormatFlags |= StringFormatFlags.NoWrap;
        // Keep a consistent weight and size for 0–99%; reserve space for 100%.
        using var measuringFont = new Font("Segoe UI", size, FontStyle.Regular, GraphicsUnit.Pixel);
        var reference = percent == 100 ? "100" : "88";
        var measured = graphics.MeasureString(reference, measuringFont, PointF.Empty, format);
        var fontSize = Math.Min(size * 0.72f, size * (size - 1f) / measured.Width);
        using var font = new Font("Segoe UI", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        var bounds = graphics.MeasureString(text, font, PointF.Empty, format);
        var origin = new PointF((float)Math.Floor((size - bounds.Width) / 2),
            (float)Math.Floor((size - bounds.Height) / 2));
        using var brush = new SolidBrush(Color.White);
        graphics.DrawString(text, font, brush, origin, format);
        var handle = bitmap.GetHicon();
        try { using var borrowed = Icon.FromHandle(handle); var old = icon.Icon; icon.Icon = (Icon)borrowed.Clone(); old?.Dispose(); }
        finally { DestroyIcon(handle); }
    }
    public void Dispose() { icon.Visible = false; icon.Icon?.Dispose(); icon.ContextMenuStrip?.Dispose(); icon.Dispose(); }
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string className, string? windowName);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int GetSystemMetricsForDpi(int index, uint dpi);
}
