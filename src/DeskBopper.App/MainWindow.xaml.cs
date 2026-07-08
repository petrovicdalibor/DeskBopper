using System.Windows;

namespace DeskBopper.App;

/// <summary>
/// The character window: a borderless, transparent, always-on-top, taskbar-less shell.
/// Audio wiring (AudioEngine + CharacterAnimator) is attached in T015; window-style
/// interop for click-through/alt-tab hiding is added in T020.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        MoveToDefaultCorner();
    }

    /// <summary>Parks the character near the bottom-right of the primary working area.</summary>
    private void MoveToDefaultCorner()
    {
        var work = SystemParameters.WorkArea;
        const double margin = 24;
        Left = work.Right - Width - margin;
        Top = work.Bottom - Height - margin;
    }
}
