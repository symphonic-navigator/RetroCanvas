namespace RetroCanvas.Core.Text;

/// <summary>
/// 16-bit VGA text-mode character cell — mirrors real VGA text mode memory layout:
/// <list type="bullet">
///   <item>Bits 0–7: ASCII character code (CP437)</item>
///   <item>Bits 8–11: Foreground color (CGA 16-color index)</item>
///   <item>Bits 12–14: Background color (CGA 8-color index; 3 bits)</item>
///   <item>Bit 15: Blink flag (or high-intensity background, depending on <see cref="TextMode.BlinkMode"/>)</item>
/// </list>
/// </summary>
public readonly struct CharCell
{
    public readonly ushort Raw;

    public CharCell(ushort raw) => Raw = raw;

    public CharCell(char ch, Color16 fg = Color16.LightGray, Color16 bg = Color16.Black, bool blink = false)
        => Raw = (ushort)((byte)ch | ((int)fg << 8) | (((int)bg & 0x7) << 12) | (blink ? 0x8000 : 0));

    public CharCell(byte code, Color16 fg = Color16.LightGray, Color16 bg = Color16.Black, bool blink = false)
        => Raw = (ushort)(code | ((int)fg << 8) | (((int)bg & 0x7) << 12) | (blink ? 0x8000 : 0));

    public byte     CharCode   => (byte)(Raw & 0xFF);
    public Color16  Foreground => (Color16)((Raw >> 8) & 0xF);
    public Color16  Background => (Color16)((Raw >> 12) & 0x7);
    public bool     Blink      => (Raw & 0x8000) != 0;

    /// <summary>Attribute byte (upper 8 bits of the cell): fg/bg/blink packed as in VGA text RAM.</summary>
    public byte Attribute => (byte)(Raw >> 8);

    public static readonly CharCell Space = new(' ', Color16.LightGray, Color16.Black);
}

/// <summary>CGA 16-color palette indices as used in text mode attributes.</summary>
public enum Color16 : byte
{
    Black        = 0,
    Blue         = 1,
    Green        = 2,
    Cyan         = 3,
    Red          = 4,
    Magenta      = 5,
    Brown        = 6,
    LightGray    = 7,
    DarkGray     = 8,
    LightBlue    = 9,
    LightGreen   = 10,
    LightCyan    = 11,
    LightRed     = 12,
    LightMagenta = 13,
    Yellow       = 14,
    White        = 15,
}
