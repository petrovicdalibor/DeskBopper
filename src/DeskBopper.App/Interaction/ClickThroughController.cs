using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace DeskBopper.App.Interaction;

/// <summary>
/// Makes the window "click-through except over the character". Because a click-through
/// window receives no mouse events, we can't rely on WPF hover events — instead we poll
/// the cursor a few dozen times a second, hit-test it against the character's actual
/// vector geometry, and flip <see cref="WindowStyles.SetClickThrough"/> accordingly.
/// </summary>
public sealed class ClickThroughController : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT p);

    private readonly Window _window;
    private readonly Visual _hitTarget;
    private readonly DispatcherTimer _timer;
    private bool _clickThrough = true;

    /// <summary>
    /// When true the controller leaves the window interactive (used during a drag so the
    /// cursor can wander off the body mid-drag without dropping click-through on us).
    /// </summary>
    public bool Suspended { get; set; }

    public ClickThroughController(Window window, Visual hitTarget)
    {
        _window = window;
        _hitTarget = hitTarget;
        _timer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = TimeSpan.FromMilliseconds(33) // ~30 Hz
        };
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        // Begin fully click-through so the toy never blocks the desktop before the first tick.
        WindowStyles.SetClickThrough(_window, true);
        _clickThrough = true;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        bool wantInteractive = Suspended || CursorOverCharacter();
        if (wantInteractive == _clickThrough) return; // state change needed?
        _clickThrough = !wantInteractive;
        WindowStyles.SetClickThrough(_window, _clickThrough);
    }

    private bool CursorOverCharacter()
    {
        if (!GetCursorPos(out POINT p)) return false;
        Point local;
        try
        {
            local = _window.PointFromScreen(new Point(p.X, p.Y));
        }
        catch
        {
            return false; // window not yet sourced
        }

        bool hit = false;
        VisualTreeHelper.HitTest(
            _hitTarget,
            null,
            r => { hit = true; return HitTestResultBehavior.Stop; },
            new PointHitTestParameters(local));
        return hit;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }
}
