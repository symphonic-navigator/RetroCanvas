using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.SDL;

namespace RetroCanvas.Core.Vga;

/// <summary>
/// SDL2 VGA backend. Creates a scaled window, converts the 8-bit indexed FrameBuffer to
/// ARGB32 via the CLUT on each Flip(), and presents via SDL2 renderer.
/// </summary>
public sealed unsafe class VgaDriver : IDisposable
{
    private static readonly Sdl _sdl = Sdl.GetApi();

    private readonly int _width;
    private readonly int _height;
    private readonly int _scale;
    private readonly long _ticksPerFrame;

    private Window*   _window;
    private Renderer* _renderer;
    private Texture*  _texture;
    private uint[]    _argbBuffer;
    private bool      _disposed;
    private long      _nextFrameTick;

    public FrameBuffer   FrameBuffer { get; }
    public Clut          Clut        { get; }
    public CrtcRegisters Crtc        { get; }

    /// <summary>Direct pointer to the back (writable) framebuffer. Set a byte per pixel (palette index).</summary>
    public byte* BackBuffer => FrameBuffer.BackBuffer;

    /// <summary>Writable back CLUT.</summary>
    public Span<RgbColor> BackClut => Clut.BackClut;

    /// <summary>Fired once per frame before rendering. Override or assign for scanline-level tricks.</summary>
    public Action<int>? OnScanlineRetrace;

    /// <summary>True when the user closed the window or pressed Escape.</summary>
    public bool QuitRequested { get; private set; }

    public VgaDriver(VideoMode mode = VideoMode.Vga320x240x256,
                     int windowScale = 3,
                     string title = "RetroCanvas",
                     int targetFps = 70)
    {
        (int w, int h) = mode.Dimensions();
        _width  = w;
        _height = h;
        _scale  = windowScale;
        _ticksPerFrame = Stopwatch.Frequency / targetFps;

        FrameBuffer = new FrameBuffer(w, h);
        Clut        = new Clut();
        Crtc        = new CrtcRegisters();
        _argbBuffer = new uint[w * h];

        InitSdl(title);
    }

    private void InitSdl(string title)
    {
        if (_sdl.Init(Sdl.InitVideo) != 0)
            throw new InvalidOperationException($"SDL Init failed: {GetSdlError()}");

        _window = _sdl.CreateWindow(
            title,
            Sdl.WindowposCentered, Sdl.WindowposCentered,
            _width * _scale, _height * _scale,
            (uint)WindowFlags.Shown);

        if (_window == null)
            throw new InvalidOperationException($"SDL CreateWindow failed: {GetSdlError()}");

        _renderer = _sdl.CreateRenderer(_window, -1,
            (uint)(RendererFlags.Accelerated | RendererFlags.Presentvsync));

        if (_renderer == null)
            throw new InvalidOperationException($"SDL CreateRenderer failed: {GetSdlError()}");

        // Nearest-neighbor scaling for authentic pixel look
        _sdl.RenderSetLogicalSize(_renderer, _width, _height);
        _sdl.SetHint(Sdl.HintRenderScaleQuality, "0");

        _texture = _sdl.CreateTexture(_renderer,
            Sdl.PixelformatArgb8888,
            (int)TextureAccess.Streaming,
            _width, _height);

        if (_texture == null)
            throw new InvalidOperationException($"SDL CreateTexture failed: {GetSdlError()}");

        _nextFrameTick = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Poll SDL events and return false if the window was closed or Escape pressed.
    /// Call this once per frame in your main loop.
    /// </summary>
    public bool PollEvents()
    {
        Event evt;
        while (_sdl.PollEvent(&evt) != 0)
        {
            switch ((EventType)evt.Type)
            {
                case EventType.Quit:
                    QuitRequested = true;
                    break;
                case EventType.Keydown when evt.Key.Keysym.Sym == (int)KeyCode.KEscape:
                    QuitRequested = true;
                    break;
            }
        }
        return !QuitRequested;
    }

    /// <summary>
    /// Simulate vertical retrace timing: busy-wait until the target 70Hz frame boundary.
    /// Returns the elapsed ticks since the previous frame.
    /// </summary>
    public long WaitForVerticalRetrace()
    {
        long now = Stopwatch.GetTimestamp();
        long wait = _nextFrameTick - now;
        if (wait > 0)
        {
            // Sleep most of it, spin the remainder for accuracy
            long sleepTicks = wait - Stopwatch.Frequency / 1000; // leave 1ms for spin
            if (sleepTicks > 0)
                System.Threading.Thread.Sleep(TimeSpan.FromTicks(sleepTicks * TimeSpan.TicksPerSecond / Stopwatch.Frequency));
            while (Stopwatch.GetTimestamp() < _nextFrameTick)
                System.Threading.Thread.SpinWait(10);
        }
        long elapsed = Stopwatch.GetTimestamp() - (_nextFrameTick - _ticksPerFrame);
        _nextFrameTick += _ticksPerFrame;
        return elapsed;
    }

    /// <summary>
    /// Swap back ↔ front framebuffer and upload to SDL texture.
    /// Call after drawing, before Present().
    /// </summary>
    public void Flip()
    {
        FrameBuffer.Flip();
        UploadFrameBuffer();
    }

    /// <summary>
    /// Swap back ↔ front CLUT.
    /// </summary>
    public void CommitClut() => Clut.CommitClut();

    /// <summary>
    /// Present the current texture to the screen.
    /// </summary>
    public void Present()
    {
        _sdl.RenderClear(_renderer);
        _sdl.RenderCopy(_renderer, _texture, null, null);
        _sdl.RenderPresent(_renderer);
    }

    private void UploadFrameBuffer()
    {
        var front = FrameBuffer.FrontBuffer;
        var clut  = Clut.FrontClut;
        int size  = _width * _height;

        // Convert indexed → ARGB8888
        for (int i = 0; i < size; i++)
        {
            ref var c = ref clut[front[i]];
            _argbBuffer[i] = 0xFF000000u | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
        }

        fixed (uint* pixels = _argbBuffer)
            _sdl.UpdateTexture(_texture, null, pixels, _width * 4);
    }

    private string GetSdlError() => _sdl.GetErrorS();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_texture  != null) { _sdl.DestroyTexture(_texture);   _texture  = null; }
        if (_renderer != null) { _sdl.DestroyRenderer(_renderer); _renderer = null; }
        if (_window   != null) { _sdl.DestroyWindow(_window);     _window   = null; }
        _sdl.Quit();
        FrameBuffer.Dispose();
        GC.SuppressFinalize(this);
    }
}
