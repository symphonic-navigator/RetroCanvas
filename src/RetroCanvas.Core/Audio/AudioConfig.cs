namespace RetroCanvas.Core.Audio;

/// <summary>Supported sample bit depths.</summary>
public enum BitDepth
{
    /// <summary>Unsigned 8-bit (0–255) — SoundBlaster style.</summary>
    Unsigned8,
    /// <summary>Signed 16-bit (-32768–32767) — SoundBlaster 16 style.</summary>
    Signed16,
}

/// <summary>Mono vs stereo output.</summary>
public enum ChannelCount
{
    Mono   = 1,
    Stereo = 2,
}

/// <summary>
/// Audio driver configuration value type.
/// All settings are latched when <see cref="AudioDriver"/> is constructed.
/// </summary>
public readonly struct AudioConfig
{
    /// <summary>Sample rate in Hz. 22050 gives that authentic SoundBlaster feel; 44100 for CD quality.</summary>
    public int SampleRate { get; init; }

    /// <summary>Sample bit depth.</summary>
    public BitDepth BitDepth { get; init; }

    /// <summary>Number of channels.</summary>
    public ChannelCount Channels { get; init; }

    /// <summary>
    /// Number of samples per callback buffer. Smaller = lower latency, higher CPU.
    /// Powers of 2 recommended. 512 at 22050 Hz ≈ 23ms latency.
    /// </summary>
    public int BufferSize { get; init; }

    /// <summary>Default SoundBlaster-style configuration.</summary>
    public static AudioConfig SoundBlaster => new()
    {
        SampleRate = 22050,
        BitDepth   = BitDepth.Signed16,
        Channels   = ChannelCount.Mono,
        BufferSize = 512,
    };

    /// <summary>CD-quality stereo.</summary>
    public static AudioConfig CdQuality => new()
    {
        SampleRate = 44100,
        BitDepth   = BitDepth.Signed16,
        Channels   = ChannelCount.Stereo,
        BufferSize = 1024,
    };
}
