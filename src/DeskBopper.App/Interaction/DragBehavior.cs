using System;
using System.Windows;
using System.Windows.Input;

namespace DeskBopper.App.Interaction;

/// <summary>
/// Lets the user drag the character to move the window. Attaches to a UI element (the
/// mascot) and calls <see cref="Window.DragMove"/> on left-button press. Raises events
/// around the drag so the owner can suspend click-through and persist the new position.
/// </summary>
public sealed class DragBehavior
{
    private readonly Window _window;

    private DragBehavior(Window window, UIElement handle)
    {
        _window = window;
        handle.MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    /// <summary>Raised when a drag begins (mouse pressed on the character).</summary>
    public event EventHandler? DragStarting;

    /// <summary>Raised when a drag ends (mouse released).</summary>
    public event EventHandler? DragCompleted;

    public static DragBehavior Attach(Window window, UIElement handle) => new(window, handle);

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;

        DragStarting?.Invoke(this, EventArgs.Empty);
        try
        {
            _window.DragMove(); // blocks until the mouse button is released
        }
        catch (InvalidOperationException)
        {
            // DragMove throws if the button was already released; ignore.
        }
        finally
        {
            DragCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
