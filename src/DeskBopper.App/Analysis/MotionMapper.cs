using System;

namespace DeskBopper.App.Analysis;

/// <summary>
/// Turns a raw loudness envelope into a normalized <see cref="MotionSignal"/> the UI can
/// consume directly: energy in 0..1, an optional beat pulse, and a silence flag.
///
/// Pure and dependency-free. Stateful across calls (adaptive normalization + onset
/// tracking), but holds no audio-library types, so it is unit-testable and can be
/// replaced by an FFT-based mapper later without touching the rest of the app.
/// </summary>
public sealed class MotionMapper
{
    // Below this raw envelope the audio is treated as silence (~-62 dBFS on -1..1 PCM).
    private readonly float _silenceThreshold;
    // Adaptive normalization: the reference peak decays slowly so quiet passages still
    // produce visible motion and loud passages don't peg permanently.
    private readonly float _peakDecay;
    private readonly float _minPeakRef;
    // Onset/beat detection smoothing and gain.
    private readonly float _avgCoeff;
    private readonly float _beatGain;

    private float _peakRef;
    private float _avgEnergy;
    private float _prevEnergy;

    public MotionMapper(
        float silenceThreshold = 0.0008f,
        float peakDecay = 0.9995f,
        float minPeakRef = 0.02f,
        float averagingCoeff = 0.9f,
        float beatGain = 3f)
    {
        _silenceThreshold = silenceThreshold;
        _peakDecay = peakDecay;
        _minPeakRef = minPeakRef;
        _avgCoeff = averagingCoeff;
        _beatGain = beatGain;
        _peakRef = minPeakRef;
    }

    /// <summary>
    /// User-facing responsiveness multiplier. 1.0 is neutral; higher = bigger motion for
    /// the same loudness. Bound to the sensitivity setting live. Clamped to a sane range.
    /// </summary>
    public float Sensitivity { get; set; } = 1f;

    /// <summary>Maps a raw envelope value to a normalized motion signal.</summary>
    public MotionSignal Map(float rawEnvelope, long timestampMs)
    {
        if (rawEnvelope < _silenceThreshold)
        {
            // Let the normalization reference relax toward the floor during silence so the
            // first beats after silence aren't crushed.
            _peakRef = MathF.Max(_minPeakRef, _peakRef * _peakDecay);
            _avgEnergy = 0f;
            _prevEnergy = 0f;
            return new MotionSignal(Energy: 0f, BeatPulse: 0f, IsSilent: true, timestampMs);
        }

        // Adaptive peak: rises instantly to new peaks, decays slowly otherwise.
        _peakRef = MathF.Max(rawEnvelope, MathF.Max(_minPeakRef, _peakRef * _peakDecay));

        float sensitivity = Math.Clamp(Sensitivity, 0.1f, 5f);
        float energy = Math.Clamp(rawEnvelope / _peakRef * sensitivity, 0f, 1f);

        // Simple onset: positive rise of energy above its running average -> a beat pulse.
        float flux = energy - _prevEnergy;
        float beat = 0f;
        if (flux > 0f && energy > _avgEnergy)
        {
            beat = Math.Clamp(flux * _beatGain, 0f, 1f);
        }

        _avgEnergy = _avgCoeff * _avgEnergy + (1f - _avgCoeff) * energy;
        _prevEnergy = energy;

        return new MotionSignal(energy, beat, IsSilent: false, timestampMs);
    }

    /// <summary>Resets adaptive state (e.g. after an audio device change).</summary>
    public void Reset()
    {
        _peakRef = _minPeakRef;
        _avgEnergy = 0f;
        _prevEnergy = 0f;
    }
}
