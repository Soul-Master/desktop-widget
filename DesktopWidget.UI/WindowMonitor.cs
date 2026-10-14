using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DesktopWidget.UI;

// Query native screen rectangles to keep every coordinate in the virtual desktop's physical pixels.
internal sealed record WindowMonitor(IntPtr Handle, WindowMonitorSettings Settings, bool IsPrimary)
{
    public static IReadOnlyList<WindowMonitor> FindAll()
    {
        var monitors = new List<WindowMonitor>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (handle, _, _, _) =>
        {
            if (Read(handle) is { } monitor) monitors.Add(monitor);
            return true;
        }, IntPtr.Zero);
        return monitors;
    }

    public static WindowMonitor? FromWindow(IntPtr hwnd) => Read(MonitorFromWindow(hwnd, 2));

    private static WindowMonitor? Read(IntPtr handle)
    {
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (handle == IntPtr.Zero || !GetMonitorInfo(handle, ref info)) return null;
        var device = new DisplayDevice { Size = Marshal.SizeOf<DisplayDevice>() };
        // The monitor interface distinguishes physical displays even when Windows reuses DISPLAY1/2.
        var deviceId = EnumDisplayDevices(info.DeviceName, 0, ref device, 1) &&
            !string.IsNullOrWhiteSpace(device.DeviceId) ? device.DeviceId : info.DeviceName;
        return new(handle, new(deviceId, info.Bounds.Left, info.Bounds.Top,
            info.Bounds.Right - info.Bounds.Left, info.Bounds.Bottom - info.Bounds.Top,
            info.WorkArea.Left, info.WorkArea.Top,
            info.WorkArea.Right - info.WorkArea.Left, info.WorkArea.Bottom - info.WorkArea.Top, 0),
            (info.Flags & 1) != 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Bounds, WorkArea;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int Size;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rectangle, IntPtr data);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevices(string deviceName, uint index, ref DisplayDevice device, uint flags);
}
