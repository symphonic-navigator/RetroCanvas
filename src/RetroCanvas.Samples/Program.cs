using RetroCanvas.Abstractions;
using RetroCanvas.Samples;

Console.WriteLine("RetroCanvas — Scene Selector");
Console.WriteLine("  1  Plasma Demo");
Console.WriteLine("  2  Palette Cycling");
Console.WriteLine("  3  Copper Bars");
Console.WriteLine("  4  Scroller Demo");
Console.WriteLine("  5  Synth Demo");
Console.Write("Choose scene (1-5): ");

DemoScene scene = Console.ReadLine()?.Trim() switch
{
    "1" => new PlasmaDemo(),
    "2" => new PaletteCycling(),
    "3" => new CopperBars(),
    "4" => new ScrollerDemo(),
    "5" => new SynthDemo(),
    _   => new PlasmaDemo(),  // default
};

bool wantAudio = scene is SynthDemo;

var host = new DemoHost(new DemoHostConfig
{
    VideoMode       = RetroCanvas.Core.Vga.VideoMode.Vga320x240x256,
    TargetFps       = 70,
    WindowTitle     = $"RetroCanvas — {scene.GetType().Name}",
    WindowScale     = 3,
    AudioSampleRate = wantAudio ? 22050 : 0,
    AudioBufferSize = 512,
});

using (host)
    host.Run(scene);
