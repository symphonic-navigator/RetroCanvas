using RetroCanvas.Abstractions;
using RetroCanvas.Core.Audio.Synthesis;
using RetroCanvas.Core.Vga;

namespace RetroCanvas.Samples;

/// <summary>
/// Audio synthesis demo: sawtooth oscillator through a slowly-opening lowpass filter with ADSR.
/// Displays a live oscilloscope of the waveform in the framebuffer.
/// </summary>
public sealed class SynthDemo : DemoScene
{
    private const int SampleRate = 22050;

    private readonly Oscillator     _osc    = new(Waveform.Sawtooth, 220f, SampleRate);
    private readonly LowpassFilter  _filter = new(200f, 0.7f, SampleRate);
    private readonly AdsrEnvelope   _env    = new(0.01f, 0.2f, 0.6f, 0.5f, SampleRate);

    private float _filterOpenness; // 0..1, slowly rises
    private short[] _scopeBuffer = new short[320];
    private int _scopePos;
    private bool _noteOn;

    protected override void OnLoad()
    {
        _env.NoteOn();
        _noteOn = true;
    }

    protected override void FillAudioBuffer(Span<short> buffer)
    {
        // Slowly open the filter
        _filterOpenness = Math.Min(1f, _filterOpenness + 0.0001f);
        _filter.Cutoff  = 200f + _filterOpenness * 3800f;

        for (int i = 0; i < buffer.Length; i++)
        {
            float sample = _osc.Next();
            sample = _filter.Process(sample);
            sample *= _env.Next();
            short s = (short)(sample * 30000f);
            buffer[i] = s;

            // Feed oscilloscope ring buffer
            _scopeBuffer[_scopePos] = s;
            _scopePos = (_scopePos + 1) % _scopeBuffer.Length;
        }

        // Retrigger after release completes
        if (_env.State == EnvelopeState.Idle && !_noteOn)
        {
            _env.NoteOn();
            _noteOn = true;
        }
    }

    protected override void OnVerticalRetrace(IFrameBuffer fb, IClut clut)
    {
        fb.Clear(0);

        // Set up a minimal palette: black + green + white
        clut[0] = new RgbColor(0, 0, 0);
        clut[1] = new RgbColor(0, 180, 0);   // scope line
        clut[2] = new RgbColor(0, 60, 0);    // scope glow
        clut[3] = new RgbColor(60, 60, 60);  // center line

        int cy = fb.Height / 2;

        // Horizontal center line
        fb.HLine(0, cy, fb.Width, 3);

        // Oscilloscope trace
        var scope = _scopeBuffer;
        for (int x = 0; x < Math.Min(fb.Width, scope.Length); x++)
        {
            int idx = (_scopePos + x) % scope.Length;
            int y = cy - scope[idx] * (fb.Height / 2 - 4) / 32767;
            y = Math.Clamp(y, 0, fb.Height - 1);

            fb.PutPixel(x, y, 1);
            if (y > 0) fb.PutPixel(x, y - 1, 2);
            if (y < fb.Height - 1) fb.PutPixel(x, y + 1, 2);
        }

        // Trigger note-off after a while and retrigger
        if (_filterOpenness > 0.5f && _noteOn)
        {
            _env.NoteOff();
            _noteOn    = false;
            _filterOpenness = 0f;
        }
    }
}
