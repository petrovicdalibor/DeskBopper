using System;

namespace DeskBopper.App.Analysis;

/// <summary>
/// Classic attack/release envelope follower over rectified audio samples.
/// Pure and dependency-free so it can be unit tested and swapped independently.
///
/// A fast attack lets the envelope jump toward transients (kick drums, beats) while a
/// slower release makes it fall away smoothly, which reads as musical head-bob rather
/// than jitter. Coefficients are derived from time constants and the sample rate.
/// </summary>
public sealed class EnvelopeFollower
{
    private readonly float _attackCoeff;
    private readonly float _releaseCoeff;
    private float _envelope;

    /// <param name="sampleRate">Samples per second of the incoming audio.</param>
    /// <param name="attackMs">Time constant for rising signal. Small = snappy.</param>
    /// <param name="releaseMs">Time constant for falling signal. Larger = smoother decay.</param>
    public EnvelopeFollower(int sampleRate, float attackMs = 5f, float releaseMs = 150f)
    {
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        _attackCoeff = CoefficientFor(attackMs, sampleRate);
        _releaseCoeff = CoefficientFor(releaseMs, sampleRate);
    }

    /// <summary>Current envelope value (>= 0). Not yet normalized.</summary>
    public float Current => _envelope;

    /// <summary>
    /// Feeds a block of mono samples (interleaved channels should be pre-mixed to mono)
    /// and returns the envelope value after the block.
    /// </summary>
    public float ProcessBlock(ReadOnlySpan<float> samples)
    {
        float env = _envelope;
        foreach (float s in samples)
        {
            float rectified = MathF.Abs(s);
            float coeff = rectified > env ? _attackCoeff : _releaseCoeff;
            env = coeff * (env - rectified) + rectified;
        }
        _envelope = env;
        return env;
    }

    /// <summary>Feeds a single mono sample. Returns the updated envelope value.</summary>
    public float ProcessSample(float sample)
    {
        float rectified = MathF.Abs(sample);
        float coeff = rectified > _envelope ? _attackCoeff : _releaseCoeff;
        _envelope = coeff * (_envelope - rectified) + rectified;
        return _envelope;
    }

    /// <summary>Resets the follower to silence (e.g. after a device change).</summary>
    public void Reset() => _envelope = 0f;

    // One-pole smoothing coefficient: exp(-1 / (tau_seconds * sampleRate)).
    // A zero/negative time constant yields 0 (no smoothing, follow instantly).
    private static float CoefficientFor(float timeMs, int sampleRate)
    {
        if (timeMs <= 0f) return 0f;
        double tauSeconds = timeMs / 1000.0;
        return (float)Math.Exp(-1.0 / (tauSeconds * sampleRate));
    }
}
