using RetroCanvas.Abstractions;
using RetroCanvas.Core.Vga;

namespace RetroCanvas.Samples;

/// <summary>
/// Static concentric-ring image with animated CLUT rotation.
/// The framebuffer is written once; all animation is pure palette cycling.
/// </summary>
public sealed class PaletteCycling : DemoScene
{
    private bool _drawn;

    protected override void OnVerticalRetrace(IFrameBuffer fb, IClut clut)
    {
        if (!_drawn)
        {
            DrawRings(fb);
            _drawn = true;
        }

        // Rotate palette entries 1–255 (keep black at 0)
        clut.RotatePalette(1, 255);
    }

    private static void DrawRings(IFrameBuffer fb)
    {
        int cx = fb.Width  / 2;
        int cy = fb.Height / 2;
        int maxR = Math.Min(cx, cy);

        // Initialize palette: rainbow gradient 1–255
        // (done in DemoHost via clut, but here we just pre-draw the indices)

        for (int y = 0; y < fb.Height; y++)
        {
            for (int x = 0; x < fb.Width; x++)
            {
                int dx = x - cx, dy = y - cy;
                int dist = (int)MathF.Sqrt(dx * dx + dy * dy);
                byte ci = (byte)(1 + dist % 255); // never 0 (keep black border)
                fb.PutPixel(x, y, ci);
            }
        }
    }

    protected override void OnLoad()
    {
        // Palette is pre-filled during first OnVerticalRetrace
    }
}
