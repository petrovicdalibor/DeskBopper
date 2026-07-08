using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DeskBopper.App.Audio;
using DeskBopper.App.Interaction;
using DeskBopper.App.Settings;
using DeskBopper.App.View;

namespace DeskBopper.App;

/// <summary>
/// The character window: a borderless, transparent, always-on-top, taskbar-less shell
/// that hosts the mascot, drives it from live system audio, and layers on the US3
/// niceties — drag to move, click-through, a tray menu, and persisted settings.
/// </summary>
public partial class MainWindow : Window
{
    private readonly UserSettings _settings = UserSettings.Load();

    private AudioEngine? _engine;
    private CharacterAnimator? _animator;
    private ClickThroughController? _clickThrough;
    private TrayMenu? _tray;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RestorePosition();
        ApplyColorFromSettings();

        // Audio + animation (US1).
        _engine = new AudioEngine(new WasapiLoopbackSource()) { Sensitivity = (float)_settings.Sensitivity };
        _animator = new CharacterAnimator(Character, () => _engine!.Latest);
        _animator.Start();
        TryStartCapture();

        // Window behaviour (US3).
        WindowStyles.HideFromAltTab(this);

        _clickThrough = new ClickThroughController(this, Character);
        _clickThrough.HoverChanged += OnHoverChanged;
        _clickThrough.Start();

        var drag = DragBehavior.Attach(this, Character);
        drag.DragStarting += (_, _) => _clickThrough!.Suspended = true;
        drag.DragCompleted += (_, _) =>
        {
            _clickThrough!.Suspended = false;
            _settings.Left = Left;
            _settings.Top = Top;
            _settings.Save();
        };

        // Keep persisted autostart flag in sync with the actual registry state.
        _settings.Autostart = Autostart.IsEnabled();
        _tray = new TrayMenu(_settings.Sensitivity, _settings.Autostart, ToDrawing(_settings.ColorHex));
        _tray.SensitivityChanged += OnSensitivityChanged;
        _tray.AutostartChanged += OnAutostartChanged;
        _tray.PickColorRequested += OnPickColorRequested;
        _tray.QuitRequested += () => Close();
    }

    private void ApplyColorFromSettings()
    {
        try
        {
            var media = (System.Windows.Media.Color)
                System.Windows.Media.ColorConverter.ConvertFromString(_settings.ColorHex);
            Character.ApplyColor(media);
        }
        catch
        {
            // Bad hex in settings -> keep the XAML default colour.
        }
    }

    private void OnPickColorRequested()
    {
        // Defer until the tray menu has fully closed. Showing the modal dialog
        // synchronously from the menu click lets that same click dismiss it instantly.
        Dispatcher.BeginInvoke(new Action(ShowColorPicker), DispatcherPriority.ApplicationIdle);
    }

    private void ShowColorPicker()
    {
        // Stop the click-through poller from toggling window styles, and drop always-on-top
        // so the dialog isn't fighting the toy for z-order/focus.
        if (_clickThrough is not null) _clickThrough.Suspended = true;
        bool wasTopmost = Topmost;
        Topmost = false;

        try
        {
            using var dlg = new System.Windows.Forms.ColorDialog
            {
                FullOpen = true,
                AnyColor = true,
                Color = ToDrawing(_settings.ColorHex),
            };
            var owner = new Win32Window(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            if (dlg.ShowDialog(owner) != System.Windows.Forms.DialogResult.OK) return;

            System.Drawing.Color d = dlg.Color;
            _settings.ColorHex = $"#{d.R:X2}{d.G:X2}{d.B:X2}";
            _settings.Save();
            Character.ApplyColor(System.Windows.Media.Color.FromRgb(d.R, d.G, d.B));
            _tray?.UpdateIconColor(d);
        }
        finally
        {
            Topmost = wasTopmost;
            if (_clickThrough is not null) _clickThrough.Suspended = false;
        }
    }

    /// <summary>Minimal owner wrapper so the WinForms dialog is parented to our window.</summary>
    private sealed class Win32Window : System.Windows.Forms.IWin32Window
    {
        public Win32Window(IntPtr handle) => Handle = handle;
        public IntPtr Handle { get; }
    }

    private static System.Drawing.Color ToDrawing(string hex)
    {
        try { return System.Drawing.ColorTranslator.FromHtml(hex); }
        catch { return System.Drawing.Color.FromArgb(0x4C, 0x6E, 0xF5); }
    }

    private void TryStartCapture()
    {
        try
        {
            _engine!.Start();
        }
        catch (Exception)
        {
            // No audio device / capture unavailable: the character simply idles.
            // Graceful device handling + user feedback lands in T027.
        }
    }

    /// <summary>Fades the character on hover so the user can see behind it.</summary>
    private void OnHoverChanged(bool hovering)
    {
        const double dimOpacity = 0.4;
        var fade = new DoubleAnimation(hovering ? dimOpacity : 1.0, TimeSpan.FromMilliseconds(120));
        Character.BeginAnimation(OpacityProperty, fade);
    }

    private void OnSensitivityChanged(double value)
    {
        _engine!.Sensitivity = (float)value;
        _settings.Sensitivity = value;
        _settings.Save();
    }

    private void OnAutostartChanged(bool enabled)
    {
        Autostart.Set(enabled);
        _settings.Autostart = enabled;
        _settings.Save();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _settings.Left = Left;
        _settings.Top = Top;
        _settings.Save();

        _tray?.Dispose();
        _clickThrough?.Dispose();
        _animator?.Dispose();
        _engine?.Dispose();
    }

    /// <summary>
    /// Restores the saved position if it still lands on the (current) virtual desktop;
    /// otherwise parks the character at the bottom-right of the primary working area.
    /// </summary>
    private void RestorePosition()
    {
        if (_settings.Left is double l && _settings.Top is double t && IsOnScreen(l, t))
        {
            Left = l;
            Top = t;
            return;
        }

        var work = SystemParameters.WorkArea;
        const double margin = 24;
        Left = work.Right - ActualWidth - margin;
        Top = work.Bottom - ActualHeight - margin;
    }

    private bool IsOnScreen(double left, double top)
    {
        double vx = SystemParameters.VirtualScreenLeft;
        double vy = SystemParameters.VirtualScreenTop;
        double vw = SystemParameters.VirtualScreenWidth;
        double vh = SystemParameters.VirtualScreenHeight;
        const double slack = 8;
        return left >= vx - slack
               && top >= vy - slack
               && left + ActualWidth <= vx + vw + slack
               && top + ActualHeight <= vy + vh + slack;
    }
}
