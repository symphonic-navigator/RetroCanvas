using RetroCanvas.Core.Vga;

namespace RetroCanvas.Abstractions;

/// <summary>
/// Safe palette access — wraps the back CLUT of a <see cref="RetroCanvas.Core.Vga.Clut"/>.
/// </summary>
public interface IClut
{
    /// <summary>Read or write a single palette entry (0–255) in the back CLUT.</summary>
    ref RgbColor this[int index] { get; }

    /// <summary>Set a gradient across a range of palette entries.</summary>
    void SetGradient(int first, int count, RgbColor from, RgbColor to);

    /// <summary>Rotate a range of palette entries by one step (palette cycling).</summary>
    void RotatePalette(int first, int count, bool reverse = false);
}
