using System.Windows.Controls;
using System.Windows.Media;

namespace DeskBopper.App.View;

/// <summary>
/// The vector mascot. Exposes the head's rotate/translate transforms (declared in XAML)
/// so <c>CharacterAnimator</c> can drive the bob without knowing the character's internals.
/// </summary>
public partial class Character : UserControl
{
    public Character()
    {
        InitializeComponent();
    }

    /// <summary>Rotation applied to the head, in degrees (pivot at the neck).</summary>
    public RotateTransform HeadRotateTransform => HeadRotate;

    /// <summary>Positional offset applied to the head (mainly vertical bob).</summary>
    public TranslateTransform HeadOffsetTransform => HeadOffset;
}
