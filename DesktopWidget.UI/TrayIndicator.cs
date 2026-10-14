using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DesktopWidget.UI;

// Own a hidden top-level window so the host's message loop delivers tray callbacks
// and Explorer's TaskbarCreated broadcast. A message-only window misses that broadcast.
internal sealed partial class TrayIndicator : IDisposable
{
    private const uint CallbackMessage = 0x8001; // WM_APP + 1
    private const uint Select = 0x400, KeySelect = 0x401, ContextMenu = 0x7B;
    private const uint Add = 0, Modify = 1, Delete = 2, SetFocus = 3, SetVersion = 4;
    private const uint MessageFlag = 1, IconFlag = 2, TipFlag = 4, ShowTipFlag = 0x80;
    private readonly Action toggle, refresh, exit;
    private readonly WindowProcedure windowProcedure;
    private readonly string className = "DesktopWidget.TrayIndicator." + Guid.NewGuid().ToString("N");
    private readonly IntPtr instance = GetModuleHandle(null);
    private readonly uint taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    private readonly TrayIconRenderer renderer;
    private IntPtr window, icon;
    private bool added, version4, disposed;
    private int previousSize;
    private int? previousPercent;
    private string tooltip = string.Empty;

    // Construct, update and dispose on the thread that owns the callback window.
    public TrayIndicator(Action toggle, Action refresh, Action exit)
    {
        this.toggle = toggle;
        this.refresh = refresh;
        this.exit = exit;
        windowProcedure = ProcessMessage; // Keep the delegate alive until DestroyWindow completes.
        renderer = new TrayIconRenderer();
        var windowClass = new WindowClass
        {
            Size = (uint)Marshal.SizeOf<WindowClass>(),
            Procedure = Marshal.GetFunctionPointerForDelegate(windowProcedure),
            Instance = instance,
            ClassName = className
        };
        try
        {
            if (RegisterClassEx(ref windowClass) == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            window = CreateWindowEx(0, className, "Codex - Usage Limits", 0x80000000,
                0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, instance, IntPtr.Zero);
            if (window == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            Update(null, "Codex - Usage Limits\nConnecting…");
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Update(int? percent, string tooltip)
    {
        if (disposed) return;
        var newTooltip = tooltip.Length > 127 ? tooltip[..127] : tooltip;
        var changed = this.tooltip != newTooltip;
        this.tooltip = newTooltip;
        // Use the taskbar's small-icon size, including when its DPI changes.
        var taskbar = FindWindow("Shell_TrayWnd", null);
        var dpi = taskbar != IntPtr.Zero ? GetDpiForWindow(taskbar) : 96;
        var size = Math.Max(16, GetSystemMetricsForDpi(49, dpi == 0 ? 96 : dpi));
        if (icon == IntPtr.Zero || percent != previousPercent || size != previousSize)
        {
            var next = renderer.Render(percent, size);
            var old = icon;
            icon = next;
            previousPercent = percent;
            previousSize = size;
            changed = true;
            if (old != IntPtr.Zero) DestroyIcon(old);
        }
        // Failed shell operations are retried even when the text and percentage are unchanged.
        if (changed || !added) PublishIcon();
    }

    private NotifyIconData Data() => new()
    {
        Size = (uint)Marshal.SizeOf<NotifyIconData>(), Window = window, Id = 1,
        Flags = MessageFlag | IconFlag | TipFlag | ShowTipFlag,
        Callback = CallbackMessage, Icon = icon, Tip = tooltip,
        Info = string.Empty, InfoTitle = string.Empty
    };

    private void PublishIcon()
    {
        if (disposed || icon == IntPtr.Zero) return;
        var data = Data();
        if (added && ShellNotifyIcon(Modify, ref data)) return;
        added = ShellNotifyIcon(Add, ref data);
        if (added)
        {
            data.Version = 4;
            version4 = ShellNotifyIcon(SetVersion, ref data);
        }
    }

    private IntPtr ProcessMessage(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam)
    {
        // No managed exception may escape a reverse P/Invoke callback into user32.
        try
        {
            if (!disposed && taskbarCreated != 0 && message == taskbarCreated)
            {
                added = false;
                PublishIcon();
                return IntPtr.Zero;
            }
            if (!disposed && message == CallbackMessage)
            {
                var notification = (uint)(lParam.ToInt64() & 0xFFFF);
                if (version4 ? notification is Select or KeySelect : notification == 0x202)
                    toggle();
                else if (version4 ? notification == ContextMenu : notification == 0x205)
                    ShowMenu(wParam);
                return IntPtr.Zero;
            }
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }
        return DefWindowProc(hwnd, message, wParam, lParam);
    }

    private void ShowMenu(UIntPtr coordinates)
    {
        var menu = CreatePopupMenu();
        if (menu == IntPtr.Zero) return;
        uint command = 0;
        try
        {
            if (!AppendMenu(menu, 0, (UIntPtr)1, "Show / hide widget") ||
                !AppendMenu(menu, 0, (UIntPtr)2, "Refresh limits") ||
                !AppendMenu(menu, 0x800, UIntPtr.Zero, null) ||
                !AppendMenu(menu, 0, (UIntPtr)3, "Exit")) return;
            var packed = coordinates.ToUInt64();
            var point = new NativePoint { X = (short)(packed & 0xFFFF), Y = (short)((packed >> 16) & 0xFFFF) };
            // Version 4 supplies the icon/click position; the legacy protocol does not.
            if (!version4 || (point.X == -1 && point.Y == -1)) GetCursorPos(out point);
            SetForegroundWindow(window);
            command = TrackPopupMenu(menu, 0x100 | 0x2, point.X, point.Y, 0, window, IntPtr.Zero);
            // Required by the shell's popup-menu contract to dismiss correctly on subsequent clicks.
            PostMessage(window, 0, UIntPtr.Zero, IntPtr.Zero);
            if (command == 0)
            {
                var data = Data();
                ShellNotifyIcon(SetFocus, ref data);
            }
        }
        finally { DestroyMenu(menu); }
        // Destroy the menu before an action can close the widget and dispose this window.
        switch (command)
        {
            case 1: toggle(); break;
            case 2: refresh(); break;
            case 3: exit(); break;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (window != IntPtr.Zero)
        {
            var data = Data();
            ShellNotifyIcon(Delete, ref data);
            DestroyWindow(window);
            window = IntPtr.Zero;
        }
        if (icon != IntPtr.Zero) { DestroyIcon(icon); icon = IntPtr.Zero; }
        UnregisterClass(className, instance);
        renderer.Dispose();
        GC.KeepAlive(windowProcedure);
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WindowProcedure(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {
        public uint Size, Style;
        public IntPtr Procedure;
        public int ClassExtra, WindowExtra;
        public IntPtr Instance, Icon, Cursor, Background;
        public string? MenuName;
        public string ClassName;
        public IntPtr SmallIcon;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint Size;
        public IntPtr Window;
        public uint Id, Flags, Callback;
        public IntPtr Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State, StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags;
        public Guid Guid;
        public IntPtr BalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X, Y; }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern ushort RegisterClassEx(ref WindowClass windowClass);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnregisterClass(string className, IntPtr instance);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateWindowEx(uint extendedStyle, string className, string title, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyWindow(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr DefWindowProc(IntPtr window, uint message, UIntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string message);
    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", ExactSpelling = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ShellNotifyIcon(uint operation, ref NotifyIconData data);
    [DllImport("user32.dll")] private static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool AppendMenu(IntPtr menu, uint flags, UIntPtr id, string? text);
    [DllImport("user32.dll")] private static extern uint TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr window, IntPtr rectangle);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyMenu(IntPtr menu);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PostMessage(IntPtr window, uint message, UIntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyIcon(IntPtr handle);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string className, string? windowName);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int GetSystemMetricsForDpi(int index, uint dpi);
}
