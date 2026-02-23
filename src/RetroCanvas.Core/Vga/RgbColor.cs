using System.Runtime.InteropServices;

namespace RetroCanvas.Core.Vga;

/// <summary>3-byte packed RGB color — mirrors VGA DAC register layout (6-bit values scaled to 8-bit).</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RgbColor : IEquatable<RgbColor>
{
    public byte R;
    public byte G;
    public byte B;

    public RgbColor(byte r, byte g, byte b) { R = r; G = g; B = b; }

    /// <summary>Create from 6-bit VGA DAC values (0–63), as used in real VGA CLUT programming.</summary>
    public static RgbColor FromVga6(byte r6, byte g6, byte b6)
        => new((byte)(r6 * 255 / 63), (byte)(g6 * 255 / 63), (byte)(b6 * 255 / 63));

    /// <summary>Create from HSV (0–360, 0–1, 0–1) — handy for palette generation.</summary>
    public static RgbColor FromHsv(float h, float s, float v)
    {
        h %= 360f;
        if (h < 0) h += 360f;
        float c = v * s;
        float x = c * (1f - MathF.Abs(h / 60f % 2f - 1f));
        float m = v - c;
        float r, g, b;
        if      (h < 60)  { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else              { r = c; g = 0; b = x; }
        return new((byte)((r + m) * 255f), (byte)((g + m) * 255f), (byte)((b + m) * 255f));
    }

    public static RgbColor Black   => new(0, 0, 0);
    public static RgbColor White   => new(255, 255, 255);
    public static RgbColor Red     => new(255, 0, 0);
    public static RgbColor Green   => new(0, 255, 0);
    public static RgbColor Blue    => new(0, 0, 255);

    public bool Equals(RgbColor other) => R == other.R && G == other.G && B == other.B;
    public override bool Equals(object? obj) => obj is RgbColor c && Equals(c);
    public override int GetHashCode() => HashCode.Combine(R, G, B);
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}
