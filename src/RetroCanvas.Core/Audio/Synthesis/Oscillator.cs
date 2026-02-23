namespace RetroCanvas.Core.Audio.Synthesis;

/// <summary>Waveform shapes supported by <see cref="Oscillator"/>.</summary>
public enum Waveform
{
    Sine,
    Square,
    Sawtooth,
    Triangle,
    Noise,
}

/// <summary>
/// Single-sample oscillator — computes one float (-1..+1) per call to <see cref="Next"/>.
/// Fully composable: feed its output into a filter, envelope, or another oscillator.
/// </summary>
public sealed class Oscillator
{
    private float _phase;        // 0..1
    private float _phaseIncrement;
    private readonly Random _rng = new();
    private float _noiseSample;

    public Waveform Waveform   { get; set; }
    public float    Frequency  { get; set; }
    public int      SampleRate { get; }

    /// <summary>Detune in cents (100 cents = 1 semitone). Applied additively to frequency.</summary>
    public float DetuneCents { get; set; }

    public Oscillator(Waveform waveform, float frequency, int sampleRate)
    {
        Waveform   = waveform;
        Frequency  = frequency;
        SampleRate = sampleRate;
        _phaseIncrement = frequency / sampleRate;
    }

    /// <summary>Compute one sample, advance phase, return value in -1..+1.</summary>
    public float Next()
    {
        float f = Frequency + DetuneCents / 100f * Frequency / 12f;
        _phaseIncrement = f / SampleRate;

        float sample = Waveform switch
        {
            Waveform.Sine     => MathF.Sin(_phase * MathF.Tau),
            Waveform.Square   => _phase < 0.5f ? 1f : -1f,
            Waveform.Sawtooth => _phase * 2f - 1f,
            Waveform.Triangle => _phase < 0.5f
                                    ? _phase * 4f - 1f
                                    : 3f - _phase * 4f,
            Waveform.Noise    => GetNoise(),
            _                 => 0f,
        };

        _phase = (_phase + _phaseIncrement) % 1f;
        return sample;
    }

    /// <summary>Reset phase to zero (for hard sync effects).</summary>
    public void Reset() => _phase = 0f;

    /// <summary>Set frequency from a MIDI note number (A4 = 69 = 440Hz).</summary>
    public void SetMidiNote(int note)
        => Frequency = 440f * MathF.Pow(2f, (note - 69) / 12f);

    private float GetNoise()
    {
        // New noise sample per cycle (phase reset detection)
        if (_phase + _phaseIncrement >= 1f || _phase == 0f)
            _noiseSample = (float)(_rng.NextDouble() * 2.0 - 1.0);
        return _noiseSample;
    }
}
