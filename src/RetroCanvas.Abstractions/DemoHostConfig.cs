using RetroCanvas.Core.Vga;

namespace RetroCanvas.Abstractions;

/// <summary>Configuration for <see cref="DemoHost"/>.</summary>
public sealed class DemoHostConfig
{
    /// <summary>VGA video mode. Default: 320×240 @ 256 colors (Mode X).</summary>
    public VideoMode VideoMode   { get; init; } = VideoMode.Vga320x240x256;

    /// <summary>Target frame rate. 70 = authentic VGA refresh rate.</summary>
    public int       TargetFps   { get; init; } = 70;

    /// <summary>Window title bar text.</summary>
    public string    WindowTitle { get; init; } = "RetroCanvas";

    /// <summary>Integer upscale factor (1 = native, 3 = 960×720 for 320×240).</summary>
    public int       WindowScale { get; init; } = 3;

    /// <summary>Sample rate for the audio driver. 0 = audio disabled.</summary>
    public int       AudioSampleRate { get; init; } = 22050;

    /// <summary>Audio buffer size in samples. Smaller = lower latency.</summary>
    public int       AudioBufferSize  { get; init; } = 512;
}
