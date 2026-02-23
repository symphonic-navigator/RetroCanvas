namespace RetroCanvas.Core.Vga;

/// <summary>
/// 256-entry Color Look-Up Table (palette) — double-buffered, mirroring VGA dual-port DAC.
/// Write to the back CLUT; call CommitClut() to make it visible on next flip.
/// </summary>
public sealed class Clut
{
    private RgbColor[] _front = new RgbColor[256];
    private RgbColor[] _back  = new RgbColor[256];

    /// <summary>Writable palette — back buffer. Index range 0–255.</summary>
    public Span<RgbColor> BackClut  => _back;

    /// <summary>Active display palette — front buffer. Consumed by VgaDriver during upload.</summary>
    public Span<RgbColor> FrontClut => _front;

    /// <summary>Indexed access to back CLUT.</summary>
    public ref RgbColor this[int index] => ref _back[index];

    /// <summary>Swap back and front CLUT. Call after you've updated the palette.</summary>
    public void CommitClut() => (_front, _back) = (_back, _front);

    /// <summary>Copy the current front CLUT into the back CLUT so you can edit from it.</summary>
    public void PullFrontToBack() => _front.CopyTo(_back, 0);

    /// <summary>Rotate the back CLUT by one entry (palette cycling — classic effect).</summary>
    /// <param name="first">First index in the cycling range.</param>
    /// <param name="count">Number of entries to cycle.</param>
    /// <param name="reverse">Rotate in reverse direction.</param>
    public void RotatePalette(int first, int count, bool reverse = false)
    {
        if (count < 2) return;
        if (!reverse)
        {
            var saved = _back[first + count - 1];
            for (int i = first + count - 1; i > first; i--)
                _back[i] = _back[i - 1];
            _back[first] = saved;
        }
        else
        {
            var saved = _back[first];
            for (int i = first; i < first + count - 1; i++)
                _back[i] = _back[i + 1];
            _back[first + count - 1] = saved;
        }
    }

    /// <summary>Fill a range of back CLUT entries with a smooth gradient.</summary>
    public void SetGradient(int first, int count, RgbColor from, RgbColor to)
    {
        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0f;
            _back[first + i] = new RgbColor(
                (byte)(from.R + (to.R - from.R) * t),
                (byte)(from.G + (to.G - from.G) * t),
                (byte)(from.B + (to.B - from.B) * t));
        }
    }
}
