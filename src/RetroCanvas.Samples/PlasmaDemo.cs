using RetroCanvas.Abstractions;
using RetroCanvas.Core.Vga;

namespace RetroCanvas.Samples;

/// <summary>
/// Classic sine plasma with palette cycling.
/// The plasma is computed into a fixed 256-value lookup table; the animation comes purely
/// from rotating the palette — no framebuffer rewrite per frame.
/// </summary>
public sealed class PlasmaDemo : DemoScene
{
    private byte[]? _plasma;
    private int _paletteOffset;

    protected override void OnLoad()
    {
        // nothing to allocate — plasma computed on first frame
    }

    protected override void OnVerticalRetrace(IFrameBuffer fb, IClut clut)
    {
        if (_plasma == null)
            BuildPlasma(fb.Width, fb.Height, fb);

        // Build a sine-rainbow palette in the back CLUT (entries 0–255)
        float t = _paletteOffset * MathF.Tau / 256f;
        for (int i = 0; i < 256; i++)
        {
            float phase = i * MathF.Tau / 256f + t;
            clut[i] = new RgbColor(
                (byte)(128 + 127 * MathF.Sin(phase)),
                (byte)(128 + 127 * MathF.Sin(phase + MathF.Tau / 3f)),
                (byte)(128 + 127 * MathF.Sin(phase + 2f * MathF.Tau / 3f)));
        }

        _paletteOffset = (_paletteOffset + 1) & 0xFF;
    }

    private void BuildPlasma(int w, int h, IFrameBuffer fb)
    {
        // Write plasma indices once — palette animation does the rest
        _plasma = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float v = MathF.Sin(x * 0.05f)
                        + MathF.Sin(y * 0.05f)
                        + MathF.Sin((x + y) * 0.035f)
                        + MathF.Sin(MathF.Sqrt(x * x + y * y) * 0.05f);

                byte ci = (byte)((v + 4f) / 8f * 255f);
                _plasma[y * w + x] = ci;
                fb.PutPixel(x, y, ci);
            }
        }
    }
}
