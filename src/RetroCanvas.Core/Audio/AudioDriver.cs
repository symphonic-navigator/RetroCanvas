using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.SDL;

namespace RetroCanvas.Core.Audio;

/// <summary>
/// SDL2 audio backend. Calls <see cref="OnBufferNeeded"/> on the audio thread
/// whenever the hardware buffer needs refilling — closest to the real SoundBlaster DMA callback.
/// </summary>
public sealed unsafe class AudioDriver : IDisposable
{
    private static readonly Sdl _sdl = Sdl.GetApi();

    // Static handle so the unmanaged callback can reach this instance.
    // One AudioDriver at a time is the expected use case.
    private static AudioDriver? _instance;

    private readonly AudioConfig _config;
    private uint _deviceId;
    private bool _started;
    private bool _disposed;

    /// <summary>
    /// Called on the audio thread when the hardware buffer needs refilling.
    /// <para><b>Thread safety:</b> This delegate is invoked from an SDL audio thread.
    /// Do not call SDL or Unity APIs from within it.</para>
    /// </summary>
    public Action<Span<short>>? OnBufferNeeded;

    public AudioConfig Config => _config;

    public AudioDriver(AudioConfig config)
    {
        _config  = config;
        _instance = this;
    }

    /// <summary>Open the audio device and start playback.</summary>
    public void Start()
    {
        if (_started) return;

        // Init SDL audio subsystem if not already done
        if ((_sdl.WasInit(Sdl.InitAudio) & Sdl.InitAudio) == 0)
            _sdl.InitSubSystem(Sdl.InitAudio);

        var spec = new AudioSpec
        {
            Freq     = _config.SampleRate,
            Format   = (ushort)(_config.BitDepth == BitDepth.Signed16 ? Sdl.AudioS16 : Sdl.AudioU8),
            Channels = (byte)(int)_config.Channels,
            Samples  = (ushort)Math.Min(_config.BufferSize, ushort.MaxValue),
            Callback = new PfnAudioCallback(&AudioCallbackImpl),
        };

        AudioSpec obtained;
        _deviceId = _sdl.OpenAudioDevice((byte*)null, 0, &spec, &obtained, 0);
        if (_deviceId == 0)
            throw new InvalidOperationException($"SDL OpenAudioDevice failed: {_sdl.GetErrorS()}");

        _sdl.PauseAudioDevice(_deviceId, 0); // unpause = start playback
        _started = true;
    }

    /// <summary>Pause / resume audio playback.</summary>
    public void SetPaused(bool paused)
    {
        if (_deviceId != 0)
            _sdl.PauseAudioDevice(_deviceId, paused ? 1 : 0);
    }

    /// <summary>Stop audio and close the device.</summary>
    public void Stop()
    {
        if (!_started || _deviceId == 0) return;
        _sdl.PauseAudioDevice(_deviceId, 1);
        _sdl.CloseAudioDevice(_deviceId);
        _deviceId = 0;
        _started  = false;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void AudioCallbackImpl(void* userdata, byte* stream, int len)
    {
        var instance = _instance;
        if (instance == null || instance.OnBufferNeeded == null)
        {
            // Silence
            NativeMemory.Fill(stream, (nuint)len, 0);
            return;
        }

        var span = new Span<short>((short*)stream, len / sizeof(short));
        try
        {
            instance.OnBufferNeeded(span);
        }
        catch
        {
            NativeMemory.Fill(stream, (nuint)len, 0);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        if (_instance == this) _instance = null;
        GC.SuppressFinalize(this);
    }
}
