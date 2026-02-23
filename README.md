# SymphonicNavigator.RetroCanvas

> 90s demoscene aesthetics for modern C# — SoundBlaster + CRTC + VGA Mode 13h with `Span<T>`, `unsafe`, and proper OOP on top.

Built for artists and coders who want plasma effects, palette cyclers, copper-bar tricks, and synthesized audio without thinking about SDL2 internals.

---

## Quick Start

```csharp
// High-level: subclass DemoScene
public class PlasmaDemo : DemoScene
{
    protected override void OnVerticalRetrace(IFrameBuffer fb, IClut clut)
    {
        for (int i = 0; i < 256; i++)
            clut[i] = RgbColor.FromHsv(i * 360f / 256f + _t, 1f, 1f);
        _t += 2f;

        for (int y = 0; y < fb.Height; y++)
        for (int x = 0; x < fb.Width;  x++)
        {
            float v = MathF.Sin(x * 0.05f) + MathF.Sin(y * 0.05f);
            fb.PutPixel(x, y, (byte)((v + 2f) / 4f * 255f));
        }
    }
    private float _t;
}

// Run it
using var host = new DemoHost(new DemoHostConfig
{
    VideoMode   = VideoMode.Vga320x240x256,
    TargetFps   = 70,
    WindowTitle = "My Demo",
    WindowScale = 3,
});
host.Run(new PlasmaDemo());
```

```csharp
// Copper bars — palette trick via per-scanline callback
protected override void OnScanlineRetrace(int line, IClut clut)
{
    int bar = (line + _scroll) / 8 % _gradients.Length;
    clut[0] = _gradients[bar][line % 8];
}
```

---

## Packages

| Package | Description |
|---|---|
| `SymphonicNavigator.RetroCanvas.Core` | Raw VGA + audio engine — `unsafe`, fast, direct |
| `SymphonicNavigator.RetroCanvas.Abstractions` | `DemoScene` / `DemoHost` OOP layer |

---

## What's included

- **VGA FrameBuffer** — unsafe double-buffered, `byte*` pointer access or safe helpers
- **CLUT (palette)** — double-buffered 256-entry palette, `SetGradient`, `RotatePalette`
- **CRTC simulation** — `OnScanlineRetrace` copper-list hook, `DisplayStart` scroll, `SplitScreen`
- **Text Mode** — CP437 font ROM, 80×25 / 80×50 / 160×100, attribute bytes, blink
- **Audio** — SDL2 callback driver, composable oscillators (Sine/Square/Saw/Triangle/Noise), biquad filters (LP/HP), ADSR envelope
- **DemoScene** — lifecycle hooks: `OnLoad`, `OnVerticalRetrace`, `OnScanlineRetrace`, `FillAudioBuffer`
- **5 sample demos** — Plasma, PaletteCycling, CopperBars, Scroller, Synth

---

## Run the samples

```bash
dotnet run --project src/RetroCanvas.Samples
```

---

## Building

```bash
dotnet build --configuration Release
```

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download) and SDL2 libraries.

**Linux:**
```bash
sudo apt install libsdl2-dev   # or equivalent
```

**macOS:**
```bash
brew install sdl2
```

**Windows:** SDL2 DLLs are bundled by Silk.NET.

---

## Versioning

Versions are driven by **MinVer** from git tags. Tag `v0.1.0` → package version `0.1.0`.

```bash
git tag v0.1.0
git push --tags
```

GitHub Actions publishes to nuget.org on version tags. Requires `NUGET_API_KEY` secret.

---

## Architecture

```
RetroCanvas.Core                RetroCanvas.Abstractions
├── Vga/                        ├── IFrameBuffer
│   ├── RgbColor                ├── IClut
│   ├── FrameBuffer (unsafe)    ├── IAudioCallback
│   ├── Clut                    ├── DemoScene  (abstract base)
│   ├── CrtcRegisters           └── DemoHost   (game loop)
│   └── VgaDriver (SDL2)
├── Audio/
│   ├── AudioDriver (SDL2)
│   └── Synthesis/
│       ├── Oscillator
│       ├── BiquadFilter
│       └── AdsrEnvelope
└── Text/
    ├── CharCell
    ├── FontRom (CP437 8×8 + 8×16)
    └── TextMode
```

---

*Co-piloted by Claude for SymphonicNavigator*
