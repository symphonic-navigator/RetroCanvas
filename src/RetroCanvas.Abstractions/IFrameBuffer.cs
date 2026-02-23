namespace RetroCanvas.Abstractions;

/// <summary>
/// Safe, index-based view of the VGA framebuffer — no pointer arithmetic needed.
/// Backed by <see cref="RetroCanvas.Core.Vga.FrameBuffer"/>.
/// </summary>
public interface IFrameBuffer
{
    int Width  { get; }
    int Height { get; }

    /// <summary>Set a single pixel (color index 0–255) in the back buffer.</summary>
    void PutPixel(int x, int y, byte colorIndex);

    /// <summary>Clear the entire back buffer to the given color index.</summary>
    void Clear(byte colorIndex = 0);

    /// <summary>Draw a horizontal line.</summary>
    void HLine(int x, int y, int length, byte colorIndex);

    /// <summary>Draw a vertical line.</summary>
    void VLine(int x, int y, int length, byte colorIndex);

    /// <summary>Fill a solid rectangle.</summary>
    void FillRect(int x, int y, int w, int h, byte colorIndex);
}
