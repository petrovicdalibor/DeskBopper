using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using H.NotifyIcon;

namespace DeskBopper.App.Interaction;

/// <summary>
/// System-tray presence and right-click menu: adjust bob sensitivity, toggle
/// start-with-Windows, and quit. The tray icon is drawn at runtime so the app ships
/// without a bundled .ico asset (see T029 for a nicer icon later).
/// </summary>
public sealed class TrayMenu : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    // Discrete sensitivity presets (within MotionMapper's 0.1..5 clamp).
    private static readonly (string Label, double Value)[] Levels =
    {
        ("Low", 0.6),
        ("Medium", 1.0),
        ("High", 1.8),
    };

    private readonly TaskbarIcon _icon;
    private readonly List<MenuItem> _levelItems = new();
    private readonly MenuItem _autostartItem;
    private IntPtr _hIcon;

    public event Action<double>? SensitivityChanged;
    public event Action<bool>? AutostartChanged;
    public event Action? QuitRequested;

    public TrayMenu(double sensitivity, bool autostart)
    {
        var menu = new ContextMenu();

        var sensitivityRoot = new MenuItem { Header = "Sensitivity" };
        foreach (var (label, value) in Levels)
        {
            var item = new MenuItem { Header = label, IsCheckable = true, Tag = value };
            item.Click += (_, _) => SelectSensitivity(value);
            _levelItems.Add(item);
            sensitivityRoot.Items.Add(item);
        }
        menu.Items.Add(sensitivityRoot);

        _autostartItem = new MenuItem { Header = "Start with Windows", IsCheckable = true, IsChecked = autostart };
        _autostartItem.Click += (_, _) => AutostartChanged?.Invoke(_autostartItem.IsChecked);
        menu.Items.Add(_autostartItem);

        menu.Items.Add(new Separator());

        var quit = new MenuItem { Header = "Quit DeskBopper" };
        quit.Click += (_, _) => QuitRequested?.Invoke();
        menu.Items.Add(quit);

        MarkClosestLevel(sensitivity);

        _hIcon = CreateTrayIconHandle();
        _icon = new TaskbarIcon
        {
            ToolTipText = "DeskBopper — bobbing to your music",
            Icon = (Icon)Icon.FromHandle(_hIcon).Clone(),
            ContextMenu = menu,
        };
        _icon.ForceCreate();
    }

    private void SelectSensitivity(double value)
    {
        MarkClosestLevel(value);
        SensitivityChanged?.Invoke(value);
    }

    private void MarkClosestLevel(double sensitivity)
    {
        // Check the preset nearest the current sensitivity.
        double best = double.MaxValue;
        MenuItem? chosen = null;
        foreach (var item in _levelItems)
        {
            double diff = Math.Abs((double)item.Tag - sensitivity);
            if (diff < best) { best = diff; chosen = item; }
        }
        foreach (var item in _levelItems)
            item.IsChecked = ReferenceEquals(item, chosen);
    }

    private static IntPtr CreateTrayIconHandle()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var head = new SolidBrush(Color.FromArgb(0x6B, 0xA7, 0xFF));
            g.FillEllipse(head, 5, 5, 22, 22);
            using var cup = new SolidBrush(Color.FromArgb(0x2B, 0x2D, 0x42));
            g.FillRectangle(cup, 2, 13, 6, 9);
            g.FillRectangle(cup, 24, 13, 6, 9);
        }
        return bmp.GetHicon();
    }

    public void Dispose()
    {
        _icon.Dispose();
        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }
    }
}
