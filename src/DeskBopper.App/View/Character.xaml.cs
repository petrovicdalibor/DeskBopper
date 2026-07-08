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
        var bodyBrush = VerticalGradient(
            Mix(baseColor, Colors.White, 0.20), baseColor, Mix(baseColor, Colors.Black, 0.22));
        var neckBrush = new SolidColorBrush(baseColor); neckBrush.Freeze();

        Color headBase = Mix(baseColor, Colors.White, 0.34);
        var headBrush = VerticalGradient(
            Mix(headBase, Colors.White, 0.22), headBase, Mix(headBase, Colors.Black, 0.12));

        var legs = new SolidColorBrush(Mix(baseColor, Colors.Black, 0.24)); legs.Freeze();

        BodyFill.Fill = bodyBrush;
        NeckFill.Fill = neckBrush;
        HeadFill.Fill = headBrush;
        LeftLegFill.Fill = legs;
        RightLegFill.Fill = legs;
    }

    private static LinearGradientBrush VerticalGradient(Color top, Color mid, Color bottom)
    {
        var b = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(0, 1),
        };
        b.GradientStops.Add(new GradientStop(top, 0));
        b.GradientStops.Add(new GradientStop(mid, 0.55));
        b.GradientStops.Add(new GradientStop(bottom, 1));
        b.Freeze();
        return b;
    }

    private static Color Mix(Color c, Color target, double amount)
    {
        byte Blend(byte a, byte b) => (byte)Math.Round(a + (b - a) * amount);
        return Color.FromRgb(Blend(c.R, target.R), Blend(c.G, target.G), Blend(c.B, target.B));
    }
}
