namespace RetroCanvas.Core.Vga;

/// <summary>Supported video modes.</summary>
public enum VideoMode
{
    /// <summary>320×200 @ 256 colors — classic VGA Mode 13h.</summary>
    Vga320x200x256,

    /// <summary>320×240 @ 256 colors — Mode X (unchained VGA, 4:3 at 70Hz).</summary>
    Vga320x240x256,

    /// <summary>640×480 @ 256 colors — extended Mode X.</summary>
    Vga640x480x256,
}

internal static class VideoModeExtensions
{
    public static (int Width, int Height) Dimensions(this VideoMode mode) => mode switch
    {
        VideoMode.Vga320x200x256 => (320, 200),
        VideoMode.Vga320x240x256 => (320, 240),
        VideoMode.Vga640x480x256 => (640, 480),
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}
