using System.Diagnostics;
using RetroCanvas.Core.Audio;
using RetroCanvas.Core.Audio.Synthesis;
using RetroCanvas.Core.Vga;

namespace RetroCanvas.Abstractions;

/// <summary>
/// Wires together <see cref="RetroCanvas.Core.Vga.VgaDriver"/> and <see cref="AudioDriver"/>,
/// runs the game loop at the configured frame rate, and dispatches scene hooks in the correct order:
/// <list type="number">
///   <item><see cref="DemoScene.OnVerticalRetrace"/> — draw the frame</item>
///   <item><see cref="DemoScene.OnScanlineRetrace"/> × N — copper-list per-scanline palette tricks</item>
///   <item>Flip framebuffer + commit CLUT → upload to SDL</item>
///   <item>Present</item>
/// </list>
/// </summary>
public sealed class DemoHost : IDisposable
{
    private readonly DemoHostConfig _config;
    private VgaDriver?  _vga;
    private AudioDriver? _audio;
    private bool        _disposed;

    public DemoHostConfig Config => _config;

    public DemoHost(DemoHostConfig? config = null)
        => _config = config ?? new DemoHostConfig();

    /// <summary>
    /// Run the demo loop with the given scene. Blocks until the window is closed or Escape pressed.
    /// </summary>
    public void Run(DemoScene scene)
    {
        _vga = new VgaDriver(
            _config.VideoMode,
            _config.WindowScale,
            _config.WindowTitle,
            _config.TargetFps);

        var fbAdapter   = new FrameBufferAdapter(_vga.FrameBuffer);
        var clutAdapter = new ClutAdapter(_vga.Clut);

        if (_config.AudioSampleRate > 0)
        {
            _audio = new AudioDriver(new AudioConfig
            {
                SampleRate = _config.AudioSampleRate,
                BitDepth   = BitDepth.Signed16,
                Channels   = ChannelCount.Mono,
                BufferSize = _config.AudioBufferSize,
            });
            _audio.OnBufferNeeded = span => scene.FillAudioBuffer(span);
            _audio.Start();
        }

        scene.OnLoad();
        long frameCount = 0;

        while (_vga.PollEvents())
        {
            // 1. Vertical retrace: let the scene draw its frame
            scene.OnVerticalRetrace(fbAdapter, clutAdapter);

            // 2. CRTC copper simulation: fire per-scanline retrace callbacks
            int scanlines = _vga.FrameBuffer.Height;
            for (int line = 0; line < scanlines; line++)
                scene.OnScanlineRetrace(line, clutAdapter);

            // 3. Flip framebuffer + commit CLUT → upload to SDL texture
            _vga.Flip();
            _vga.CommitClut();

            // 4. Present
            _vga.Present();

            // 5. Frame timing (skip if PresentVSync already throttled us)
            _vga.WaitForVerticalRetrace();

            frameCount++;
        }

        scene.OnUnload();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _audio?.Dispose();
        _vga?.Dispose();
    }

    // -------------------------------------------------------------------------
    // Adapters — safe wrappers over Core types
    // -------------------------------------------------------------------------

    private sealed class FrameBufferAdapter : IFrameBuffer
    {
        private readonly RetroCanvas.Core.Vga.FrameBuffer _fb;
        public FrameBufferAdapter(RetroCanvas.Core.Vga.FrameBuffer fb) => _fb = fb;

        public int Width  => _fb.Width;
        public int Height => _fb.Height;

        public void PutPixel(int x, int y, byte colorIndex) => _fb.PutPixel(x, y, colorIndex);
        public void Clear(byte colorIndex = 0)               => _fb.Clear(colorIndex);
        public void HLine(int x, int y, int length, byte colorIndex) => _fb.HLine(x, y, length, colorIndex);
        public void VLine(int x, int y, int length, byte colorIndex) => _fb.VLine(x, y, length, colorIndex);
        public void FillRect(int x, int y, int w, int h, byte colorIndex) => _fb.FillRect(x, y, w, h, colorIndex);
    }

    private sealed class ClutAdapter : IClut
    {
        private readonly RetroCanvas.Core.Vga.Clut _clut;
        public ClutAdapter(RetroCanvas.Core.Vga.Clut clut) => _clut = clut;

        public ref RgbColor this[int index] => ref _clut[index];
        public void SetGradient(int first, int count, RgbColor from, RgbColor to)
            => _clut.SetGradient(first, count, from, to);
        public void RotatePalette(int first, int count, bool reverse = false)
            => _clut.RotatePalette(first, count, reverse);
    }
}
