using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DeskBopper.App.Interaction;

/// <summary>
/// Thin P/Invoke helpers over the window's extended styles:
/// <list type="bullet">
/// <item>Hide from Alt-Tab / task switcher (tool window).</item>
/// <item>Toggle click-through so the character never blocks the desktop except when the
/// cursor is actually over its body.</item>
/// </list>
/// </summary>
public static class WindowStyles
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_LAYERED = 0x00080000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static IntPtr Handle(Window window) => new WindowInteropHelper(window).Handle;

    /// <summary>Removes the window from Alt-Tab and the taskbar switcher.</summary>
    public static void HideFromAltTab(Window window)
    {
        IntPtr hwnd = Handle(window);
        if (hwnd == IntPtr.Zero) return;
        long ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        ex |= WS_EX_TOOLWINDOW;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex));
    }

    /// <summary>
    /// Turns click-through on/off. When on, mouse events fall through to whatever is
    /// behind the window; when off, the window is interactive (draggable, right-clickable).
    /// </summary>
    public static void SetClickThrough(Window window, bool enabled)
    {
        IntPtr hwnd = Handle(window);
        if (hwnd == IntPtr.Zero) return;
        long ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        if (enabled)
            ex |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
        else
            ex &= ~(long)WS_EX_TRANSPARENT;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex));
    }
}
