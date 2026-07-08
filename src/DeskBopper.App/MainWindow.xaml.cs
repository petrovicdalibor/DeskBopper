using System;
using System.Windows;
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

        // Audio + animation (US1).
        _engine = new AudioEngine(new WasapiLoopbackSource()) { Sensitivity = (float)_settings.Sensitivity };
        _animator = new CharacterAnimator(Character, () => _engine!.Latest);
        _animator.Start();
        TryStartCapture();

        // Window behaviour (US3).
        WindowStyles.HideFromAltTab(this);

        _clickThrough = new ClickThroughController(this, Character);
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
        _tray = new TrayMenu(_settings.Sensitivity, _settings.Autostart);
        _tray.SensitivityChanged += OnSensitivityChanged;
        _tray.AutostartChanged += OnAutostartChanged;
        _tray.QuitRequested += () => Close();
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
