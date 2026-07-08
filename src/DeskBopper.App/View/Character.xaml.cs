using System;
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

    /// <summary>Hip rotation for the left leg+foot group (degrees).</summary>
    public RotateTransform LeftLegRotateTransform => LeftLegRotate;

    /// <summary>Hip rotation for the right leg+foot group (degrees).</summary>
    public RotateTransform RightLegRotateTransform => RightLegRotate;

    /// <summary>Vertical hop offset for the left leg+foot group.</summary>
    public TranslateTransform LeftLegOffsetTransform => LeftLegOffset;

    /// <summary>Vertical hop offset for the right leg+foot group.</summary>
    public TranslateTransform RightLegOffsetTransform => RightLegOffset;

    /// <summary>
    /// Recolours the character from a single base colour: the body/neck take the base,
    /// the head a lighter shade, and the legs a darker shade, so the whole guy stays
    /// cohesive. Headphones, shades, and feet keep their dark accent.
    /// </summary>
    public void ApplyColor(Color baseColor)
    {
        var body = new SolidColorBrush(baseColor);
        var head = new SolidColorBrush(Mix(baseColor, Colors.White, 0.32));
        var legs = new SolidColorBrush(Mix(baseColor, Colors.Black, 0.22));
        body.Freeze(); head.Freeze(); legs.Freeze();

        BodyFill.Fill = body;
        NeckFill.Fill = body;
        HeadFill.Fill = head;
        LeftLegFill.Fill = legs;
        RightLegFill.Fill = legs;
    }

    private static Color Mix(Color c, Color target, double amount)
    {
        byte Blend(byte a, byte b) => (byte)Math.Round(a + (b - a) * amount);
        return Color.FromRgb(Blend(c.R, target.R), Blend(c.G, target.G), Blend(c.B, target.B));
    }
}
