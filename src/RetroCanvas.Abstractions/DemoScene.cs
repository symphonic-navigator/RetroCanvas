namespace RetroCanvas.Abstractions;

/// <summary>
/// Abstract base class for all RetroCanvas demo scenes.
/// Subclass this and override the hooks you need — <see cref="DemoHost"/> calls them
/// in the correct order each frame.
/// </summary>
public abstract class DemoScene
{
    /// <summary>Called once when the scene is first loaded. Allocate resources here.</summary>
    protected internal virtual void OnLoad() { }

    /// <summary>Called once when the scene is unloaded. Free resources here.</summary>
    protected internal virtual void OnUnload() { }

    /// <summary>
    /// Called once per frame at the start of vertical blank, before rendering.
    /// Draw your frame into <paramref name="fb"/> and update the palette via <paramref name="clut"/>.
    /// </summary>
    protected internal virtual void OnVerticalRetrace(IFrameBuffer fb, IClut clut) { }

    /// <summary>
    /// Called once per active scanline (0..239 at 320×240) during the frame.
    /// This is the copper-list hook: change palette entries here to create
    /// per-scanline color effects, just like the Amiga copper or VGA CRTC tricks.
    /// </summary>
    /// <param name="line">Current scanline number (0 = top of screen).</param>
    /// <param name="clut">Live palette — changes take effect for subsequent scanlines on screen.</param>
    protected internal virtual void OnScanlineRetrace(int line, IClut clut) { }

    /// <summary>
    /// Called on the audio thread when the hardware buffer needs refilling.
    /// Override this to produce audio. Fill <paramref name="buffer"/> with signed 16-bit samples.
    /// </summary>
    protected internal virtual void FillAudioBuffer(Span<short> buffer)
        => buffer.Clear(); // silence by default
}
