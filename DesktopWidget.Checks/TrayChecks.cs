using DesktopWidget.UI;
using System.Runtime.InteropServices;
using System.Text;

// Exercise the real Win32/GDI+ calls without starting WinUI or contacting Codex.
internal static class TrayChecks
{
    public static void Run()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Tray checks require Windows.");
        using var renderer = new TrayIconRenderer();
        foreach (var size in new[] { 16, 20, 24, 32, 48 })
        {
            var images = new List<byte[]>();
            foreach (var percent in new int?[] { null, 0, 8, 88, 100 })
            {
                var icon = renderer.Render(percent, size);
                try
                {
                    var pixels = ReadPixels(icon, size);
                    var alpha = pixels.Where((_, index) => index % 4 == 3).ToArray();
                    Check(alpha.Any(value => value != 0) && alpha.Any(value => value == 0),
                        $"Tray {percent?.ToString() ?? "dash"} at {size}px has visible text and transparency");
                    Check(!images.Any(previous => previous.SequenceEqual(pixels)), "Different percentages produce different icons");
                    images.Add(pixels);
                    var repeated = renderer.Render(percent, size);
                    try { Check(ReadPixels(repeated, size).SequenceEqual(pixels), "Repeated icon rendering is stable"); }
                    finally { DestroyIcon(repeated); }
                }
                finally { DestroyIcon(icon); }
            }
        }
        var before = GetGuiResources(GetCurrentProcess(), 0);
        for (var i = 0; i < 200; i++)
        {
            var icon = renderer.Render(i % 101, 24);
            DestroyIcon(icon);
        }
        Check(GetGuiResources(GetCurrentProcess(), 0) <= before + 2, "Repeated rendering does not leak GDI handles");

        // Synchronous tests stay on the thread owning the hidden window.
        var toggles = 0;
        var refreshes = 0;
        var exits = 0;
        using var tray = new TrayIndicator(() => toggles++, () => refreshes++, () => exits++);
        var window = FindTrayWindow();
        Check(window != IntPtr.Zero && !IsWindowVisible(window), "Tray owns a hidden top-level callback window");
        tray.Update(88, new string('x', 300));
        tray.Update(88, new string('x', 300));
        var identity = new IconIdentifier { Size = (uint)Marshal.SizeOf<IconIdentifier>(), Window = window, Id = 1 };
        Check(ShellNotifyIconGetRect(ref identity, out _) == 0, "Shell registers the tray icon and accepts a truncated tooltip");
        SendMessage(window, 0x8001, UIntPtr.Zero, (IntPtr)0x10400);
        SendMessage(window, 0x8001, UIntPtr.Zero, (IntPtr)0x10401);
        Check(toggles == 2, "Mouse and keyboard selections invoke show/hide exactly once");
        SendMessage(window, 0x8001, UIntPtr.Zero, (IntPtr)0x10202);
        Check(toggles == 2, "Version 4 does not also toggle on legacy mouse-up");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        SendMessage(window, 0x8001, UIntPtr.Zero, (IntPtr)0x10400);
        Check(toggles == 3, "Window callback remains rooted after garbage collection");

        ChooseMenuItem(window, 0);
        ChooseMenuItem(window, 1);
        ChooseMenuItem(window, 2);
        Check(toggles == 4 && refreshes == 1 && exits == 1,
            $"Native context menu preserves show/hide, refresh, separator, and exit actions (toggle={toggles}, refresh={refreshes}, exit={exits})");

        // Remove only this test icon, then simulate Explorer's broadcast to the owner.
        var data = new DeleteIconData { Size = (uint)Marshal.SizeOf<DeleteIconData>(), Window = window, Id = 1 };
        Check(ShellNotifyIcon(2, ref data), "Test icon can be removed independently of other applications");
        SendMessage(window, RegisterWindowMessage("TaskbarCreated"), UIntPtr.Zero, IntPtr.Zero);
        Check(ShellNotifyIconGetRect(ref identity, out _) == 0, "TaskbarCreated restores the icon without restarting the widget");
        tray.Update(null, "Unavailable\nAwaiting refreshed limit");
        tray.Dispose();
        tray.Dispose();
        tray.Update(50, "Ignored after disposal");
        Check(!IsWindow(window) && FindTrayWindow() == IntPtr.Zero, "Disposal is idempotent and destroys the callback window");
        Check(ShellNotifyIconGetRect(ref identity, out _) != 0, "Disposal removes the shell icon");
        using var another = new TrayIndicator(() => { }, () => { }, () => { });
        Check(FindTrayWindow() != IntPtr.Zero, "Tray can be created again after disposal");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }

    private static byte[] ReadPixels(IntPtr icon, int size)
    {
        Check(GetIconInfo(icon, out var info), "Rendered icon has valid native bitmap handles");
        try
        {
            Check(GetObject(info.Color, Marshal.SizeOf<NativeBitmap>(), out var bitmap) != 0 &&
                bitmap.Width == size && bitmap.Height == size && bitmap.BitsPixel == 32,
                "Rendered icon preserves the requested physical size and alpha format");
            var pixels = new byte[bitmap.WidthBytes * bitmap.Height];
            Check(GetBitmapBits(info.Color, pixels.Length, pixels) == pixels.Length, "Rendered pixels are readable");
            return pixels;
        }
        finally
        {
            DeleteObject(info.Color);
            DeleteObject(info.Mask);
        }
    }

    private static IntPtr FindTrayWindow()
    {
        var result = IntPtr.Zero;
        EnumThreadWindows(GetCurrentThreadId(), (window, _) =>
        {
            var name = new StringBuilder(128);
            GetClassName(window, name, name.Capacity);
            if (!name.ToString().StartsWith("DesktopWidget.TrayIndicator.", StringComparison.Ordinal)) return true;
            result = window;
            return false;
        }, IntPtr.Zero);
        return result;
    }

    private static void ChooseMenuItem(IntPtr window, int index)
    {
        // Complete focus and dismissal messages from the preceding synthetic click.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            while (PeekMessage(out var pending, IntPtr.Zero, 0, 0, 1))
            {
                TranslateMessage(ref pending);
                DispatchMessage(ref pending);
            }
            Thread.Sleep(10);
        }
        var thread = GetCurrentThreadId();
        // Click only this test's popup window, after its menu loop starts.
        // The bounded cancellation path prevents a failed check from leaving a popup open.
        var selection = Task.Run(() =>
        {
            var deadline = Environment.TickCount64 + 3000;
            while (Environment.TickCount64 < deadline)
            {
                var info = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
                if (GetGUIThreadInfo(thread, ref info) && info.MenuOwner == window && (info.Flags & 4) != 0)
                {
                    var popup = IntPtr.Zero;
                    EnumThreadWindows(thread, (candidate, _) =>
                    {
                        var name = new StringBuilder(128);
                        GetClassName(candidate, name, name.Capacity);
                        if (name.ToString() != "#32768" || !IsWindowVisible(candidate)) return true;
                        popup = candidate;
                        return false;
                    }, IntPtr.Zero);
                    if (popup != IntPtr.Zero)
                    {
                        var item = new MenuBarInfo { Size = (uint)Marshal.SizeOf<MenuBarInfo>() };
                        if (GetMenuBarInfo(popup, -4, index == 2 ? 4 : index + 1, ref item) && item.Rect.Bottom > item.Rect.Top)
                        {
                            var point = new NativePoint { X = (item.Rect.Left + item.Rect.Right) / 2, Y = (item.Rect.Top + item.Rect.Bottom) / 2 };
                            ScreenToClient(popup, ref point);
                            var position = (IntPtr)((point.Y << 16) | (point.X & 0xFFFF));
                            PostMessage(popup, 0x200, UIntPtr.Zero, position);
                            PostMessage(popup, 0x201, (UIntPtr)1, position);
                            PostMessage(popup, 0x202, UIntPtr.Zero, position);
                            return;
                        }
                    }
                }
                Thread.Sleep(10);
            }
            PostMessage(window, 0x1F, UIntPtr.Zero, IntPtr.Zero); // WM_CANCELMODE
            throw new Exception("Test tray menu did not open.");
        });
        SendMessage(window, 0x8001, UIntPtr.Zero, (IntPtr)0x1007B);
        selection.GetAwaiter().GetResult();
    }

    [StructLayout(LayoutKind.Sequential)] private struct NativeBitmap
    {
        public int Type, Width, Height, WidthBytes;
        public ushort Planes, BitsPixel;
        public IntPtr Bits;
    }
    [StructLayout(LayoutKind.Sequential)] private struct IconInfo
    {
        public int IsIcon;
        public uint HotspotX, HotspotY;
        public IntPtr Mask, Color;
    }
    [StructLayout(LayoutKind.Sequential)] private struct IconIdentifier
    {
        public uint Size;
        public IntPtr Window;
        public uint Id;
        public Guid Guid;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct DeleteIconData
    {
        public uint Size;
        public IntPtr Window;
        public uint Id, Flags, Callback;
        public IntPtr Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string? Tip;
        public uint State, StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string? Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string? InfoTitle;
        public uint InfoFlags;
        public Guid Guid;
        public IntPtr BalloonIcon;
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct MenuBarInfo
    {
        public uint Size;
        public NativeRect Rect;
        public IntPtr Menu, Window;
        public uint Focus;
    }
    [StructLayout(LayoutKind.Sequential)] private struct GuiThreadInfo
    {
        public uint Size, Flags;
        public IntPtr Active, Focus, Capture, MenuOwner, MoveSize, Caret;
        public NativeRect CaretRect;
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeMessage
    {
        public IntPtr Window;
        public uint Id;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int X, Y;
        public uint Private;
    }
    private delegate bool EnumerateWindow(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetIconInfo(IntPtr icon, out IconInfo info);
    [DllImport("gdi32.dll", EntryPoint = "GetObjectW", ExactSpelling = true)] private static extern int GetObject(IntPtr handle, int size, out NativeBitmap bitmap);
    [DllImport("gdi32.dll")] private static extern int GetBitmapBits(IntPtr bitmap, int count, [Out] byte[] pixels);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DeleteObject(IntPtr handle);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyIcon(IntPtr icon);
    [DllImport("user32.dll")] private static extern uint GetGuiResources(IntPtr process, uint flags);
    [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool EnumThreadWindows(uint thread, EnumerateWindow callback, IntPtr parameter);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder name, int capacity);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr SendMessage(IntPtr window, uint message, UIntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PostMessage(IntPtr window, uint message, UIntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetGUIThreadInfo(uint thread, ref GuiThreadInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMenuBarInfo(IntPtr window, int objectId, int itemId, ref MenuBarInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ScreenToClient(IntPtr window, ref NativePoint point);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PeekMessage(out NativeMessage message, IntPtr window, uint minimum, uint maximum, uint remove);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool TranslateMessage(ref NativeMessage message);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr DispatchMessage(ref NativeMessage message);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindow(IntPtr window);
    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconGetRect", ExactSpelling = true)] private static extern int ShellNotifyIconGetRect(ref IconIdentifier identity, out NativeRect rectangle);
    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", ExactSpelling = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ShellNotifyIcon(uint operation, ref DeleteIconData data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string message);
}
