using System;
using System.Windows.Media;
using DeskBopper.App.Analysis;
using DeskBopper.App.View;

namespace DeskBopper.App.View;

/// <summary>
/// Drives the character's head every frame from the latest <see cref="MotionSignal"/>.
/// Runs on the WPF render loop (<see cref="CompositionTarget.Rendering"/>) so motion is
/// smooth and stays on the UI thread; it only ever *reads* the newest signal, never
/// blocks. Idle behaviour (when silent) is layered on in US2.
/// </summary>
public sealed class CharacterAnimator : IDisposable
{
    private readonly Character _character;
    private readonly Func<MotionSignal> _signal;

    // --- Tunables (adjusted for feel in T016) ---
    private const double BaseBobHz = 1.6;    // oscillation speed at low energy
    private const double EnergyBobHz = 2.6;  // extra speed added at full energy
    private const double MaxBobPixels = 16;  // peak vertical travel
    private const double BeatBobPixels = 10; // extra downward kick on a beat
    private const double MaxTiltDegrees = 7; // peak head tilt
    private const double MaxLegDegrees = 16; // peak leg swing at the hip
    private const double BeatLegDegrees = 9; // extra leg kick on a beat
    private const double LegHopPixels = 4;   // small alternating foot lift
    private const double EnergyAttack = 0.35; // how fast smoothed energy rises per frame
    private const double EnergyRelease = 0.08; // how fast it falls
    private const double BeatDecay = 0.86;   // per-frame decay of the beat kick

    private double _phase;
    private double _smoothedEnergy;
    private double _beat;
    private TimeSpan _lastRenderTime = TimeSpan.MinValue;
    private bool _running;

    public CharacterAnimator(Character character, Func<MotionSignal> signalProvider)
    {
        _character = character ?? throw new ArgumentNullException(nameof(character));
        _signal = signalProvider ?? throw new ArgumentNullException(nameof(signalProvider));
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        CompositionTarget.Rendering += OnRendering;
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        double dt = ComputeDelta(e);
        if (dt <= 0) return;

        MotionSignal s = _signal();

        // Smooth the energy so per-buffer noise doesn't make the head twitch.
        double target = s.IsSilent ? 0.0 : s.Energy;
        double rate = target > _smoothedEnergy ? EnergyAttack : EnergyRelease;
        _smoothedEnergy += (target - _smoothedEnergy) * rate;

        // Decaying beat kick, re-triggered by onset pulses.
        _beat = Math.Max(_beat * BeatDecay, s.IsSilent ? 0.0 : s.BeatPulse);

        // Advance the bob oscillator; livelier music oscillates faster.
        double hz = BaseBobHz + EnergyBobHz * _smoothedEnergy;
        _phase += dt * hz * 2.0 * Math.PI;
        if (_phase > Math.PI * 2.0) _phase -= Math.PI * 2.0;

        double bob = Math.Sin(_phase) * MaxBobPixels * _smoothedEnergy
                     + _beat * BeatBobPixels;
        double tilt = Math.Sin(_phase * 0.5) * MaxTiltDegrees * _smoothedEnergy;

        _character.HeadOffsetTransform.Y = Clamp(bob, -MaxBobPixels, MaxBobPixels + BeatBobPixels);
        _character.HeadRotateTransform.Angle = Clamp(tilt, -MaxTiltDegrees, MaxTiltDegrees);

        // Legs march in anti-phase (left forward while right swings back) with a small
        // alternating hop, so he taps his feet in time with the music.
        double sinP = Math.Sin(_phase);
        double legSwing = sinP * MaxLegDegrees * _smoothedEnergy + _beat * BeatLegDegrees;
        double legMax = MaxLegDegrees + BeatLegDegrees;
        _character.LeftLegRotateTransform.Angle = Clamp(legSwing, -legMax, legMax);
        _character.RightLegRotateTransform.Angle = Clamp(-legSwing, -legMax, legMax);
        _character.LeftLegOffsetTransform.Y = -Math.Max(0, sinP) * LegHopPixels * _smoothedEnergy;
        _character.RightLegOffsetTransform.Y = -Math.Max(0, -sinP) * LegHopPixels * _smoothedEnergy;
    }

    private double ComputeDelta(EventArgs e)
    {
        if (e is not RenderingEventArgs args) return 1.0 / 60.0;
        TimeSpan now = args.RenderingTime;
        if (_lastRenderTime == TimeSpan.MinValue)
        {
            _lastRenderTime = now;
            return 1.0 / 60.0;
        }
        double dt = (now - _lastRenderTime).TotalSeconds;
        _lastRenderTime = now;
        // Guard against pauses/first frames producing huge jumps.
        return dt is > 0 and < 0.1 ? dt : 1.0 / 60.0;
    }

    private static double Clamp(double v, double min, double max) =>
        v < min ? min : v > max ? max : v;

    public void Dispose() => Stop();
}
