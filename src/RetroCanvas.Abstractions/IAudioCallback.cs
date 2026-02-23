namespace RetroCanvas.Abstractions;

/// <summary>
/// Optional audio callback interface for <see cref="DemoScene"/> subclasses that produce audio.
/// Implement this on your scene to hook into <see cref="DemoHost"/>'s audio driver.
/// </summary>
public interface IAudioCallback
{
    /// <summary>
    /// Called on the SDL audio thread when the hardware buffer needs refilling.
    /// Fill <paramref name="buffer"/> with signed 16-bit samples (-32768..32767).
    /// <para><b>Thread safety:</b> This runs on a separate thread. Avoid allocations.</para>
    /// </summary>
    void FillAudioBuffer(Span<short> buffer);
}
