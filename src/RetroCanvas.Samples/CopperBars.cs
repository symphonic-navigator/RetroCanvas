using RetroCanvas.Abstractions;
using RetroCanvas.Core.Vga;

namespace RetroCanvas.Samples;

/// <summary>
/// Classic copper bars demo using <see cref="DemoScene.OnScanlineRetrace"/>.
/// Each frame we paint a dark gradient background, then the copper-list changes
/// palette entry 0 (the background color) on specific scanlines to produce the bars.
/// This replicates the 90s trick: the Amiga copper or VGA CRTC interrupt would
/// swap the background color between scanlines mid-frame.
/// </summary>
public sealed class CopperBars : DemoScene
{
    private const int NumBars = 4;
    private const int BarHeight = 16;
    private const int GradientSteps = 8;

    private readonly RgbColor[][] _barGradients = BuildGradients();
    private int _scrollOffset;

    protected override void OnLoad()
    {
        // Background is palette entry 0 — starts black
    }

    protected override void OnVerticalRetrace(IFrameBuffer fb, IClut clut)
    {
        // Clear screen to color 0 (the background — copper will animate it)
        fb.Clear(0);

        // Advance scroll
        _scrollOffset = (_scrollOffset + 2) % (fb.Height + NumBars * BarHeight);
    }

    protected override void OnScanlineRetrace(int line, IClut clut)
    {
        // For each bar, compute which gradient row this scanline falls on
        // and swap palette entry 0 accordingly — instant copper bar effect.
        var newColor = RgbColor.Black;

        for (int b = 0; b < NumBars; b++)
        {
            int barCenter = (b * (200 / NumBars) + _scrollOffset) % 240;
            int barTop    = barCenter - BarHeight / 2;
            int relLine   = line - barTop;
            if (relLine >= 0 && relLine < BarHeight)
            {
                int gradRow = relLine < GradientSteps
                    ? relLine
                    : relLine >= BarHeight - GradientSteps
                        ? BarHeight - 1 - relLine
                        : GradientSteps - 1;
                gradRow = Math.Clamp(gradRow, 0, GradientSteps - 1);
                newColor = _barGradients[b % _barGradients.Length][gradRow];
                break;
            }
        }

        clut[0] = newColor;
    }

    private static RgbColor[][] BuildGradients()
    {
        // Four bars in classic demoscene colors
        (RgbColor peak, RgbColor edge)[] colors =
        [
            (new RgbColor(255,  64,  64), new RgbColor( 60,   0,   0)),  // red
            (new RgbColor( 64, 255,  64), new RgbColor(  0,  60,   0)),  // green
            (new RgbColor( 64,  64, 255), new RgbColor(  0,   0,  60)),  // blue
            (new RgbColor(255, 255,  64), new RgbColor( 60,  60,   0)),  // yellow
        ];

        var gradients = new RgbColor[colors.Length][];
        for (int i = 0; i < colors.Length; i++)
        {
            gradients[i] = new RgbColor[GradientSteps];
            for (int g = 0; g < GradientSteps; g++)
            {
                float t = (float)g / (GradientSteps - 1);
                gradients[i][g] = new RgbColor(
                    (byte)(colors[i].edge.R + (colors[i].peak.R - colors[i].edge.R) * t),
                    (byte)(colors[i].edge.G + (colors[i].peak.G - colors[i].edge.G) * t),
                    (byte)(colors[i].edge.B + (colors[i].peak.B - colors[i].edge.B) * t));
            }
        }
        return gradients;
    }
}
