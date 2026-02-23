namespace RetroCanvas.Core.Vga;

/// <summary>
/// Simulated CRTC (CRT Controller) registers. Values are latched by VgaDriver
/// at the correct timing points — DisplayStart at vertical blank, SplitScreen per scanline.
/// </summary>
public sealed class CrtcRegisters
{
    /// <summary>Byte offset into the framebuffer where display starts. Used for hardware scrolling.</summary>
    public int DisplayStart { get; private set; }

    /// <summary>
    /// Scanline at which the display address counter resets to 0.
    /// Set to Height (e.g. 240) to disable. Classic trick: game area top, status bar bottom.
    /// </summary>
    public int SplitScreen { get; private set; } = int.MaxValue;

    /// <summary>Palette index for the border/overscan area around the active display region.</summary>
    public byte OverscanColor { get; private set; }

    /// <summary>Set byte offset (within framebuffer) where the display starts — hardware horizontal/vertical scroll.</summary>
    public void SetDisplayStart(int offset)     => DisplayStart   = offset;

    /// <summary>Set the scanline at which the display resets to address 0 (split-screen trick).</summary>
    public void SetSplitScreen(int scanline)    => SplitScreen    = scanline;

    /// <summary>Set the palette index used for the overscan border area.</summary>
    public void SetOverscanColor(byte colorIndex) => OverscanColor = colorIndex;
}
