namespace RetroCanvas.Core.Audio;

/// <summary>
/// Double-buffered sample buffer for lockless audio production.
/// One thread fills the back buffer; the audio driver reads the front buffer.
/// </summary>
public sealed class SampleBuffer
{
    private short[] _front;
    private short[] _back;

    public int BufferSize { get; }

    public SampleBuffer(int bufferSize)
    {
        BufferSize = bufferSize;
        _front = new short[bufferSize];
        _back  = new short[bufferSize];
    }

    /// <summary>Writable span for the back buffer — fill this each callback.</summary>
    public Span<short> BackBuffer => _back;

    /// <summary>Read-only span of the front buffer — consumed by the audio driver.</summary>
    public ReadOnlySpan<short> FrontBuffer => _front;

    /// <summary>Swap back and front buffers.</summary>
    public void Commit() => (_front, _back) = (_back, _front);
}
