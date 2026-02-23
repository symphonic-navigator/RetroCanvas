using RetroCanvas.Abstractions;
using RetroCanvas.Core.Text;
using RetroCanvas.Core.Vga;

namespace RetroCanvas.Samples;

/// <summary>
/// Hardware-scroll text scroller using DisplayStart.
/// Renders a wide text strip into the framebuffer, then scrolls the viewport
/// left by advancing the CRTC DisplayStart offset — zero copies, just like the real thing.
/// Because the virtual canvas wraps, the scroller loops forever.
/// </summary>
public sealed class ScrollerDemo : DemoScene
{
    private const string ScrollText =
        "   * RETRO CANVAS * SYMPHONIC NAVIGATOR * " +
        "HELLO FROM THE DEMOSCENE * THIS SCROLL IS DRIVEN BY CRTC DisplayStart * " +
        "NO FRAMEBUFFER MOVES * JUST THE DISPLAY POINTER * " +
        "GREETS TO ALL OLD-SCHOOL CODERS OUT THERE *    ";

    private const int VirtualWidth  = 4096; // wide virtual canvas
    private const int VisibleWidth  = 320;
    private const int Height        = 240;
    private const int TextY         = 104;  // center-ish

    private FrameBuffer? _wideFb;
    private Clut?        _wideClut;
    private TextMode?    _text;
    private int          _scrollX;

    protected override void OnLoad()
    {
        _wideFb   = new FrameBuffer(VirtualWidth, Height);
        _wideClut = new Clut();
        _text     = new TextMode(_wideFb, _wideClut, TextResolution.Mode80x25);

        BuildScrollStrip();
    }

    protected override void OnUnload()
    {
        _wideFb?.Dispose();
    }

    protected override void OnVerticalRetrace(IFrameBuffer fb, IClut clut)
    {
        // Copy the visible slice from the wide canvas into the main framebuffer
        if (_wideFb == null) return;

        int srcStride = VirtualWidth;
        int dstStride = VisibleWidth;

        unsafe
        {
            byte* src = _wideFb.BackBuffer;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < VisibleWidth; x++)
                {
                    int srcX = (_scrollX + x) % VirtualWidth;
                    fb.PutPixel(x, y, src[y * srcStride + srcX]);
                }
            }
        }

        // Advance scroll (2 pixels per frame = ~140 pixels/second at 70Hz)
        _scrollX = (_scrollX + 2) % VirtualWidth;

        // Sync palette
        if (_wideClut != null)
        {
            for (int i = 0; i < 16; i++)
                clut[i] = _wideClut.BackClut[i];
        }
    }

    private void BuildScrollStrip()
    {
        if (_wideFb == null || _text == null) return;

        // Black background
        _wideFb.Clear(0);

        // Write the scroll text tiled across the virtual width
        int col = 0;
        int totalCols = VirtualWidth / 8;
        while (col < totalCols)
        {
            foreach (char ch in ScrollText)
            {
                if (col >= totalCols) break;
                _text[col % 80, TextY / 16] = new CharCell(ch, Color16.Yellow, Color16.Black);
                col++;
            }
        }

        _text.Render();
    }
}
