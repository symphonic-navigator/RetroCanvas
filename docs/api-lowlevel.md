# RetroCanvas — Low-Level API (`RetroCanvas.Core`)

## VGA

### `RgbColor`
3-byte packed RGB struct. Use `FromVga6()` for authentic 6-bit DAC values, `FromHsv()` for palette generation.

### `FrameBuffer`
Unsafe double-buffered pixel canvas. Each byte is a palette index (0–255).

```csharp
unsafe
{
    byte* fb = vga.BackBuffer;
    fb[y * 320 + x] = colorIndex;   // put pixel
}
// or via the safe methods:
vga.FrameBuffer.PutPixel(x, y, ci);
vga.FrameBuffer.HLine(x, y, length, ci);
vga.FrameBuffer.FillRect(x, y, w, h, ci);
vga.FrameBuffer.Clear();
```

### `Clut`
256-entry Color Look-Up Table, double-buffered.

```csharp
vga.BackClut[i] = new RgbColor(r, g, b);
vga.CommitClut();   // swap back → front

// Helpers
vga.Clut.SetGradient(0, 128, RgbColor.Black, RgbColor.White);
vga.Clut.RotatePalette(1, 255);  // palette cycling
```

### `CrtcRegisters`
Hardware-scroll simulation.

```csharp
vga.Crtc.SetDisplayStart(offset);      // byte offset into framebuffer
vga.Crtc.SetSplitScreen(scanline: 200);
vga.Crtc.SetOverscanColor(0);
```

### `VgaDriver`
SDL2 backend — creates a scaled window and handles the frame loop.

```csharp
using var vga = new VgaDriver(VideoMode.Vga320x240x256, windowScale: 3);

while (vga.PollEvents())
{
    // draw...
    vga.WaitForVerticalRetrace();
    vga.Flip();
    vga.CommitClut();
    vga.Present();
}
```

---

## Audio

### `AudioDriver`
SDL2 audio callback driver.

```csharp
var audio = new AudioDriver(AudioConfig.SoundBlaster);
audio.OnBufferNeeded += (Span<short> buffer) =>
{
    for (int i = 0; i < buffer.Length; i++)
        buffer[i] = synth.NextSample();
};
audio.Start();
```

### Synthesis primitives (composable, per-sample)

```csharp
var osc    = new Oscillator(Waveform.Sawtooth, 440f, 22050);
var filter = new LowpassFilter(1200f, 0.7f, 22050);
var env    = new AdsrEnvelope(0.01f, 0.1f, 0.6f, 0.3f, 22050);

short NextSample()
{
    float s = osc.Next();          // -1..+1
    s = filter.Process(s);         // filtered
    s *= env.Next();               // shaped
    return (short)(s * 32767f);
}
```

---

## Text Mode

```csharp
var text = new TextMode(fb, clut, TextResolution.Mode80x25);
text.Print(0, 0, "Hello, Demoscene!", Color16.Yellow, Color16.Black);
text.Render(frameCount);
```
