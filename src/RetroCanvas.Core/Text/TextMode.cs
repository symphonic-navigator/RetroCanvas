using RetroCanvas.Core.Vga;

namespace RetroCanvas.Core.Text;

/// <summary>Supported text mode resolutions.</summary>
public enum TextResolution
{
    /// <summary>80×25 chars using 8×16 glyphs → 640×400 framebuffer pixels.</summary>
    Mode80x25,
    /// <summary>80×50 chars using 8×8 glyphs → 640×400 framebuffer pixels.</summary>
    Mode80x50,
    /// <summary>160×100 "chunky PETSCII" mode using 2×4 pixel blocks.</summary>
    Mode160x100,
}

/// <summary>Controls the VGA sequencer's 9th-pixel behaviour (only relevant in 9-dot character mode).</summary>
public enum CharWidthMode
{
    /// <summary>9th pixel copies the 8th — creates smooth connected box-drawing characters.</summary>
    Replicate,
    /// <summary>9th pixel is background color — gap between characters.</summary>
    Background,
}

/// <summary>Whether attribute bit 15 means blink or high-intensity background.</summary>
public enum BlinkModeFlag
{
    /// <summary>Bit 15 → text blink.</summary>
    Blink,
    /// <summary>Bit 15 → high-intensity background (8 extra bg colors).</summary>
    HighIntensityBackground,
}

/// <summary>
/// Renders a <see cref="CharCell"/> grid into a VGA <see cref="FrameBuffer"/>.
/// Manages its own palette mapping: color indices 0–15 map to CGA colors in the CLUT.
/// </summary>
public sealed class TextMode
{
    private readonly FrameBuffer _fb;
    private readonly Clut _clut;
    private readonly TextResolution _resolution;
    private readonly CharCell[] _cells;

    private int _cols;
    private int _rows;
    private int _charW;
    private int _charH;
    private bool _cursorVisible;
    private int _cursorCol;
    private int _cursorRow;

    public int Columns => _cols;
    public int Rows    => _rows;

    public CharWidthMode CharWidthMode { get; set; } = CharWidthMode.Replicate;
    public BlinkModeFlag BlinkMode     { get; set; } = BlinkModeFlag.Blink;

    /// <summary>Palette index base for the 16 CGA text colors (default: 0).</summary>
    public int PaletteBase { get; set; } = 0;

    // CGA 16-color default palette
    private static readonly RgbColor[] CgaPalette =
    [
        new(0,   0,   0),   // 0  Black
        new(0,   0,   170), // 1  Blue
        new(0,   170, 0),   // 2  Green
        new(0,   170, 170), // 3  Cyan
        new(170, 0,   0),   // 4  Red
        new(170, 0,   170), // 5  Magenta
        new(170, 85,  0),   // 6  Brown
        new(170, 170, 170), // 7  LightGray
        new(85,  85,  85),  // 8  DarkGray
        new(85,  85,  255), // 9  LightBlue
        new(85,  255, 85),  // 10 LightGreen
        new(85,  255, 255), // 11 LightCyan
        new(255, 85,  85),  // 12 LightRed
        new(255, 85,  255), // 13 LightMagenta
        new(255, 255, 85),  // 14 Yellow
        new(255, 255, 255), // 15 White
    ];

    public TextMode(FrameBuffer fb, Clut clut, TextResolution resolution = TextResolution.Mode80x25)
    {
        _fb         = fb;
        _clut       = clut;
        _resolution = resolution;

        (_cols, _rows, _charW, _charH) = resolution switch
        {
            TextResolution.Mode80x25  => (80, 25, 8, 16),
            TextResolution.Mode80x50  => (80, 50, 8,  8),
            TextResolution.Mode160x100 => (160, 100, 2, 2),
            _ => throw new ArgumentOutOfRangeException(nameof(resolution))
        };

        _cells = new CharCell[_cols * _rows];
        Array.Fill(_cells, CharCell.Space);

        InitPalette();
    }

    private void InitPalette()
    {
        for (int i = 0; i < 16; i++)
            _clut[PaletteBase + i] = CgaPalette[i];
        _clut.CommitClut();
        _clut.PullFrontToBack();
    }

    /// <summary>Indexed access to character cells.</summary>
    public ref CharCell this[int col, int row] => ref _cells[row * _cols + col];

    /// <summary>Write a string starting at (col, row) with the given colors.</summary>
    public void Print(int col, int row, string text,
                      Color16 fg = Color16.LightGray, Color16 bg = Color16.Black)
    {
        foreach (char ch in text)
        {
            if (col >= _cols) { col = 0; row++; }
            if (row >= _rows) break;
            _cells[row * _cols + col] = new CharCell(ch, fg, bg);
            col++;
        }
    }

    /// <summary>Clear all cells to space with the given colors.</summary>
    public void Clear(Color16 fg = Color16.LightGray, Color16 bg = Color16.Black)
        => Array.Fill(_cells, new CharCell(' ', fg, bg));

    /// <summary>Set cursor position (not rendered, just tracked for Print helpers).</summary>
    public void SetCursor(int col, int row, bool visible = true)
    {
        _cursorCol = col;
        _cursorRow = row;
        _cursorVisible = visible;
    }

    /// <summary>
    /// Render the entire text grid into the framebuffer's back buffer.
    /// Call once per frame after updating cells.
    /// </summary>
    public void Render(long frameCount = 0)
    {
        bool blinkOn = (frameCount / 30) % 2 == 0;

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                ref var cell = ref _cells[row * _cols + col];
                RenderCell(col, row, cell, blinkOn, frameCount);
            }
        }

        if (_cursorVisible && blinkOn)
            RenderCursor();
    }

    private void RenderCell(int col, int row, CharCell cell, bool blinkOn, long frameCount)
    {
        if (_resolution == TextResolution.Mode160x100)
        {
            // Chunky mode: each cell is a 2×2 block, color = foreground index
            byte ci = (byte)(PaletteBase + (int)cell.Foreground);
            int px = col * _charW;
            int py = row * _charH;
            for (int dy = 0; dy < _charH; dy++)
                for (int dx = 0; dx < _charW; dx++)
                    if (px + dx < _fb.Width && py + dy < _fb.Height)
                        _fb.PutPixel(px + dx, py + dy, ci);
            return;
        }

        var glyphData = _charH == 8
            ? FontRom.GetGlyph8x8(cell.CharCode)
            : FontRom.GetGlyph8x16(cell.CharCode);

        bool blink = cell.Blink && BlinkMode == BlinkModeFlag.Blink;
        byte fgIdx = (byte)(PaletteBase + (int)cell.Foreground);
        byte bgIdx = (byte)(PaletteBase + (int)cell.Background);

        for (int glyphRow = 0; glyphRow < _charH; glyphRow++)
        {
            byte rowBits = glyphData[glyphRow];
            int py = row * _charH + glyphRow;
            if (py >= _fb.Height) break;

            for (int glyphCol = 0; glyphCol < _charW; glyphCol++)
            {
                int px = col * _charW + glyphCol;
                if (px >= _fb.Width) break;

                bool pixelOn = (rowBits & (0x80 >> glyphCol)) != 0;
                if (blink && !blinkOn) pixelOn = false;

                _fb.PutPixel(px, py, pixelOn ? fgIdx : bgIdx);
            }
        }
    }

    private void RenderCursor()
    {
        int py = (_cursorRow + 1) * _charH - 2;
        int px = _cursorCol * _charW;
        byte ci = (byte)(PaletteBase + (int)Color16.White);
        for (int dx = 0; dx < _charW; dx++)
            if (px + dx < _fb.Width && py < _fb.Height)
                _fb.PutPixel(px + dx, py, ci);
    }
}
