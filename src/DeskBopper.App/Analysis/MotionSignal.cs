namespace DeskBopper.App.Analysis;

/// <summary>
/// The single contract handed from the audio layer to the UI layer.
/// Immutable, allocation-free, and safe to publish across threads by value.
/// </summary>
/// <param name="Energy">Smoothed, normalized loudness in the range 0..1. Drives bob magnitude.</param>
/// <param name="BeatPulse">Short-lived 0..1 spike on a detected onset. Optional accent; 0 when no onset.</param>
/// <param name="IsSilent">True when audio is effectively silent, signalling the UI to idle.</param>
/// <param name="TimestampMs">Capture time (ms, monotonic) for latency measurement/debugging.</param>
public readonly record struct MotionSignal(
    float Energy,
    float BeatPulse,
    bool IsSilent,
    long TimestampMs)
{
    /// <summary>A neutral, silent signal used before any audio has been analyzed.</summary>
    public static MotionSignal Silence(long timestampMs = 0) =>
        new(Energy: 0f, BeatPulse: 0f, IsSilent: true, TimestampMs: timestampMs);
}
