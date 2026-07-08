using System;
using DeskBopper.App.Analysis;

namespace DeskBopper.App.Audio;

/// <summary>
/// Owns audio capture and the analysis pipeline. On the capture thread it turns each
/// buffer into a <see cref="MotionSignal"/> (envelope follow -> normalize/map) and
/// publishes the newest value to a single slot. The UI reads <see cref="Latest"/> once
/// per frame — newest-wins, no queue, so there is never backpressure or added latency.
/// </summary>
public sealed class AudioEngine : IDisposable
{
    private readonly ILoopbackCapture _capture;
    private readonly MotionMapper _mapper;

    private EnvelopeFollower? _follower;
    private int _sampleRate;

    // Boxed MotionSignal published by the capture thread and read by the UI thread.
    // A volatile reference write/read is atomic; the tiny per-buffer box is negligible.
    private volatile object _latestBox = MotionSignal.Silence();

    public AudioEngine(ILoopbackCapture capture, MotionMapper? mapper = null)
    {
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _mapper = mapper ?? new MotionMapper();
        _capture.SamplesAvailable += OnSamples;
        _capture.Stopped += OnCaptureStopped;
    }

    /// <summary>The most recent motion signal. Safe to read from any thread.</summary>
    public MotionSignal Latest => (MotionSignal)_latestBox;

    /// <summary>Live responsiveness control, forwarded to the mapper (see T025).</summary>
    public float Sensitivity
    {
        get => _mapper.Sensitivity;
        set => _mapper.Sensitivity = value;
    }

    /// <summary>
    /// Raised when capture ends. Null argument = clean stop; non-null = device
    /// removed/changed or error, so the owner can re-attach (see T027).
    /// </summary>
    public event Action<Exception?>? CaptureStopped;

    public void Start() => _capture.Start();

    public void Stop() => _capture.Stop();

    private void OnSamples(ReadOnlySpan<float> monoSamples, int sampleRate)
    {
        // (Re)build the follower if the device's sample rate changed.
        if (_follower is null || sampleRate != _sampleRate)
        {
            _sampleRate = sampleRate;
            _follower = new EnvelopeFollower(sampleRate);
            _mapper.Reset();
        }

        float envelope = _follower.ProcessBlock(monoSamples);
        MotionSignal signal = _mapper.Map(envelope, Environment.TickCount64);
        _latestBox = signal; // volatile publish, newest-wins
    }

    private void OnCaptureStopped(Exception? error)
    {
        _follower?.Reset();
        _mapper.Reset();
        _latestBox = MotionSignal.Silence(Environment.TickCount64);
        CaptureStopped?.Invoke(error);
    }

    public void Dispose()
    {
        _capture.SamplesAvailable -= OnSamples;
        _capture.Stopped -= OnCaptureStopped;
        _capture.Dispose();
    }
}
