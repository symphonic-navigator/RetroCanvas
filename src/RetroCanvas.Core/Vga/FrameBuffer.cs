using System.Runtime.InteropServices;

namespace RetroCanvas.Core.Vga;

/// <summary>
/// Unsafe double-buffered VGA framebuffer. Back buffer is your canvas; call Flip() to swap.
/// Each byte is a palette index into the CLUT (0–255).
/// </summary>
public sealed unsafe class FrameBuffer : IDisposable
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _size;

    private byte* _front;
    private byte* _back;
    private bool _disposed;

    public int Width  => _width;
    public int Height => _height;
    public int Size   => _size;

    /// <summary>Direct pointer to the back (writable) buffer.</summary>
    public byte* BackBuffer => _back;

    /// <summary>Direct pointer to the front (display) buffer — read-only by convention.</summary>
    public byte* FrontBuffer => _front;

    public FrameBuffer(int width, int height)
    {
        _width  = width;
        _height = height;
        _size   = width * height;
        _front  = (byte*)NativeMemory.AllocZeroed((nuint)_size);
        _back   = (byte*)NativeMemory.AllocZeroed((nuint)_size);
    }

    /// <summary>Write a single pixel (color index) at (x, y) into the back buffer. No bounds checking.</summary>
    public void PutPixel(int x, int y, byte colorIndex)
        => _back[y * _width + x] = colorIndex;

    /// <summary>Clear entire back buffer to the given color index.</summary>
    public void Clear(byte colorIndex = 0)
        => NativeMemory.Fill(_back, (nuint)_size, colorIndex);

    /// <summary>Fill a horizontal span into the back buffer.</summary>
    public void HLine(int x, int y, int length, byte colorIndex)
    {
        if ((uint)y >= (uint)_height) return;
        int x0 = Math.Max(x, 0);
        int x1 = Math.Min(x + length, _width);
        if (x1 <= x0) return;
        NativeMemory.Fill(_back + y * _width + x0, (nuint)(x1 - x0), colorIndex);
    }

    /// <summary>Fill a vertical span into the back buffer.</summary>
    public void VLine(int x, int y, int length, byte colorIndex)
    {
        if ((uint)x >= (uint)_width) return;
        for (int dy = 0; dy < length; dy++)
        {
            int row = y + dy;
            if ((uint)row < (uint)_height)
                _back[row * _width + x] = colorIndex;
        }
    }

    /// <summary>Fill a solid rectangle into the back buffer.</summary>
    public void FillRect(int x, int y, int w, int h, byte colorIndex)
    {
        for (int row = y; row < y + h; row++)
            HLine(x, row, w, colorIndex);
    }

    /// <summary>Blit a raw byte span directly into the back buffer (no palette conversion).</summary>
    public void Blit(ReadOnlySpan<byte> src, int destX, int destY, int srcWidth)
    {
        int rows = src.Length / srcWidth;
        for (int row = 0; row < rows; row++)
        {
            int dy = destY + row;
            if ((uint)dy >= (uint)_height) continue;
            int x0 = Math.Max(destX, 0);
            int x1 = Math.Min(destX + srcWidth, _width);
            if (x1 <= x0) continue;
            var srcRow = src.Slice(row * srcWidth + (x0 - destX), x1 - x0);
            fixed (byte* pSrc = srcRow)
                Buffer.MemoryCopy(pSrc, _back + dy * _width + x0, x1 - x0, x1 - x0);
        }
    }

    /// <summary>Swap back and front buffers — call after you've finished drawing.</summary>
    public void Flip()
    {
        byte* tmp = _front;
        _front = _back;
        _back  = tmp;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        NativeMemory.Free(_front);
        NativeMemory.Free(_back);
        _front = _back = null;
    }
}
