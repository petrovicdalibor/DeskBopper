using System;

namespace DeskBopper.App.Audio;

/// <summary>
/// Receives a block of mono, float (-1..1) samples plus the sample rate they were
/// captured at. Using a dedicated delegate (rather than <c>EventHandler&lt;T&gt;</c>)
/// lets us pass a <see cref="ReadOnlySpan{T}"/> and avoid copying the audio buffer.
/// </summary>
public delegate void MonoSamplesHandler(ReadOnlySpan<float> monoSamples, int sampleRate);

/// <summary>
/// Abstraction over "capture whatever is currently playing on the default output device".
/// Keeps the NAudio/WASAPI implementation behind a seam so the audio engine, analysis, and
/// UI never reference the capture library directly (and so capture can be faked in tests).
/// </summary>
public interface ILoopbackCapture : IDisposable
{
    /// <summary>True between a successful <see cref="Start"/> and a <see cref="Stop"/>/failure.</summary>
    bool IsCapturing { get; }

    /// <summary>
    /// Raised on the capture thread with mono samples for each buffer the device delivers.
    /// Handlers must be fast and non-blocking to preserve real-time behavior.
    /// </summary>
    event MonoSamplesHandler? SamplesAvailable;

    /// <summary>
    /// Raised when capture ends. The argument is <c>null</c> for a clean <see cref="Stop"/>
    /// and non-null when the device was removed/changed or an error occurred, so the owner
    /// can re-attach to the new default device.
    /// </summary>
    event Action<Exception?>? Stopped;

    /// <summary>Begins capturing the default render device's output mix.</summary>
    void Start();

    /// <summary>Stops capturing. Safe to call when already stopped.</summary>
    void Stop();
}
