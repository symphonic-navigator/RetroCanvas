# CRTC Simulation

## What the real CRTC did

The 6845 CRT Controller in a real VGA card managed the electron beam timing:
- **Vertical blank** (~1.3ms of the 14.3ms frame at 70Hz): framebuffer flip latched, palette swap possible
- **Horizontal blank** (240 active scanlines at 70Hz): fires between each displayed scanline
- **DisplayStart register**: byte offset into VRAM where the display starts → hardware smooth scroll
- **SplitScreen register**: scanline at which the display address resets to 0 (two independent scroll regions)

## What RetroCanvas simulates

"Vibe-accurate, not cycle-accurate."

Each frame proceeds as follows:
1. `OnVerticalRetrace` fires once → draw your frame
2. `OnScanlineRetrace(line)` fires for lines 0..Height-1 → copper-list palette tricks
3. Framebuffer and CLUT are flipped
4. SDL texture is uploaded and presented

The per-scanline callbacks happen before rendering, not interleaved with the electron beam. This means palette changes in `OnScanlineRetrace` affect the entire rendered frame uniformly per scanline — which is how you get copper bars, rainbow effects, and split-screen tricks.

## Copper bars recipe

```csharp
protected override void OnScanlineRetrace(int line, IClut clut)
{
    // Change palette entry 0 (background) per scanline:
    int barRow = line / 8 % 8;
    clut[0] = _gradient[barRow];
}
```

## DisplayStart scrolling

```csharp
// Scroll the display horizontally (byte-accurate, sub-pixel via pixel-double modes)
_scrollOffset = (_scrollOffset + 1) % (fb.Width);
vga.Crtc.SetDisplayStart(_scrollOffset);
```
