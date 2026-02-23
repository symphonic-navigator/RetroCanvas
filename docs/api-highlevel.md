# RetroCanvas — High-Level API (`RetroCanvas.Abstractions`)

## DemoScene

Subclass `DemoScene` and override the hooks you need:

```csharp
public class MyDemo : DemoScene
{
    protected override void OnLoad() { /* allocate assets */ }
    protected override void OnUnload() { /* free resources */ }

    protected override void OnVerticalRetrace(IFrameBuffer fb, IClut clut)
    {
        fb.Clear(0);
        // draw your frame...
    }

    // Called per scanline — copper-list tricks here
    protected override void OnScanlineRetrace(int line, IClut clut)
    {
        clut[0] = new RgbColor(/* background color for this scanline */);
    }

    // Called on audio thread — fill buffer with signed 16-bit samples
    protected override void FillAudioBuffer(Span<short> buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = _synth.NextSample();
    }
}
```

## DemoHost

```csharp
var host = new DemoHost(new DemoHostConfig
{
    VideoMode       = VideoMode.Vga320x240x256,
    TargetFps       = 70,
    WindowTitle     = "My Demo",
    WindowScale     = 3,            // 320×240 → 960×720
    AudioSampleRate = 22050,        // 0 = audio disabled
    AudioBufferSize = 512,
});

using (host)
    host.Run(new MyDemo());
```

## Interfaces

| Interface | Description |
|---|---|
| `IFrameBuffer` | Safe pixel-writing surface (no pointers) |
| `IClut` | Palette access — `clut[i]`, `SetGradient`, `RotatePalette` |
| `IAudioCallback` | Implement alongside `DemoScene` for audio production |

## Hook call order per frame

1. `OnVerticalRetrace(fb, clut)` — draw the frame
2. `OnScanlineRetrace(line, clut)` × N — copper tricks
3. Framebuffer flip + CLUT commit
4. Upload to SDL texture + present
5. Frame timing (vsync-equivalent wait)
