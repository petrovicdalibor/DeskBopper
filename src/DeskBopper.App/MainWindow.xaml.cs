using System;
using System.Windows;
using DeskBopper.App.Audio;
using DeskBopper.App.View;

namespace DeskBopper.App;

/// <summary>
/// The character window: a borderless, transparent, always-on-top, taskbar-less shell
/// that hosts the mascot and drives it from live system audio. Window-style interop for
/// click-through/alt-tab hiding is added in T020.
/// </summary>
public partial class MainWindow : Window
{
    private AudioEngine? _engine;
    private CharacterAnimator? _animator;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        MoveToDefaultCorner();

        _engine = new AudioEngine(new WasapiLoopbackSource());
        _animator = new CharacterAnimator(Character, () => _engine!.Latest);
        _animator.Start();

        try
        {
            _engine.Start();
        }
        catch (Exception)
        {
            // No audio device / capture unavailable: the character simply idles.
            // Graceful device handling + user feedback lands in T027.
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _animator?.Dispose();
        _engine?.Dispose();
    }

    /// <summary>Parks the character near the bottom-right of the primary working area.</summary>
    private void MoveToDefaultCorner()
    {
        var work = SystemParameters.WorkArea;
        const double margin = 24;
        Left = work.Right - ActualWidth - margin;
        Top = work.Bottom - ActualHeight - margin;
    }
}
