using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DeskBopper.App.Audio;

/// <summary>
/// <see cref="ILoopbackCapture"/> backed by NAudio's WASAPI loopback capture. Taps the
/// default render device's output mix — i.e. exactly what you hear — and down-mixes each
/// buffer to mono float samples before handing them off. Supports the two mix formats
/// WASAPI actually hands us: 32-bit IEEE float (the common case) and 16-bit PCM.
/// </summary>
public sealed class WasapiLoopbackSource : ILoopbackCapture
{
    private WasapiLoopbackCapture? _capture;
    private float[] _mono = Array.Empty<float>();
    private bool _disposed;

    public bool IsCapturing { get; private set; }

    public event MonoSamplesHandler? SamplesAvailable;
    public event Action<Exception?>? Stopped;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsCapturing) return;

        // No argument => default render device, captured in loopback.
        _capture = new WasapiLoopbackCapture();
        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += OnRecordingStopped;
        _capture.StartRecording();
        IsCapturing = true;
    }

    public void Stop()
    {
        if (_capture is null) return;
        IsCapturing = false;
        // Triggers RecordingStopped (with null exception) where we tear down.
        _capture.StopRecording();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        var handler = SamplesAvailable;
        if (handler is null || e.BytesRecorded == 0 || _capture is null) return;

        WaveFormat fmt = _capture.WaveFormat;
        int channels = Math.Max(1, fmt.Channels);
        var bytes = e.Buffer.AsSpan(0, e.BytesRecorded);

        int frames = DownmixToMono(bytes, fmt, channels, out ReadOnlySpan<float> mono);
        if (frames > 0)
        {
            handler(mono, fmt.SampleRate);
        }
    }

    /// <summary>
    /// Averages interleaved channels into <see cref="_mono"/> and returns the frame count.
    /// Returns 0 for an unsupported format rather than throwing on the capture thread.
    /// </summary>
    private int DownmixToMono(ReadOnlySpan<byte> bytes, WaveFormat fmt, int channels, out ReadOnlySpan<float> mono)
    {
        int frames;
        switch (fmt.Encoding)
        {
            case WaveFormatEncoding.IeeeFloat when fmt.BitsPerSample == 32:
            {
                var samples = MemoryMarshal.Cast<byte, float>(bytes);
                frames = samples.Length / channels;
                EnsureMono(frames);
                for (int f = 0; f < frames; f++)
                {
                    float sum = 0f;
                    int baseIdx = f * channels;
                    for (int c = 0; c < channels; c++) sum += samples[baseIdx + c];
                    _mono[f] = sum / channels;
                }
                break;
            }
            case WaveFormatEncoding.Pcm when fmt.BitsPerSample == 16:
            {
                var samples = MemoryMarshal.Cast<byte, short>(bytes);
                frames = samples.Length / channels;
                EnsureMono(frames);
                for (int f = 0; f < frames; f++)
                {
                    int sum = 0;
                    int baseIdx = f * channels;
                    for (int c = 0; c < channels; c++) sum += samples[baseIdx + c];
                    _mono[f] = sum / channels / 32768f;
                }
                break;
            }
            default:
                mono = ReadOnlySpan<float>.Empty;
                return 0;
        }

        mono = _mono.AsSpan(0, frames);
        return frames;
    }

    private void EnsureMono(int frames)
    {
        if (_mono.Length < frames) _mono = new float[frames];
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        IsCapturing = false;
        if (_capture is not null)
        {
            _capture.DataAvailable -= OnDataAvailable;
            _capture.RecordingStopped -= OnRecordingStopped;
            _capture.Dispose();
            _capture = null;
        }
        Stopped?.Invoke(e.Exception);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SamplesAvailable = null;
        Stopped = null;
        if (_capture is not null)
        {
            _capture.DataAvailable -= OnDataAvailable;
            _capture.RecordingStopped -= OnRecordingStopped;
            try { _capture.StopRecording(); } catch { /* already stopped */ }
            _capture.Dispose();
            _capture = null;
        }
        IsCapturing = false;
    }
}
