namespace RetroCanvas.Core.Audio.Synthesis;

/// <summary>
/// Second-order biquad IIR filter — the classic Chris-at-16 filter.
/// Supports lowpass and highpass topologies. One sample at a time, composable.
/// </summary>
public class BiquadFilter
{
    public enum FilterType { Lowpass, Highpass, Bandpass }

    private FilterType _type;
    private float _cutoff;
    private float _resonance;
    private readonly int _sampleRate;

    // Coefficients
    private float _b0, _b1, _b2, _a1, _a2;

    // Delay line
    private float _x1, _x2, _y1, _y2;

    public FilterType Type      { get => _type;      set { _type = value;      UpdateCoefficients(); } }
    public float      Cutoff    { get => _cutoff;    set { _cutoff = value;    UpdateCoefficients(); } }
    public float      Resonance { get => _resonance; set { _resonance = value; UpdateCoefficients(); } }

    /// <summary>Construct a lowpass filter.</summary>
    public static BiquadFilter Lowpass(float cutoff, float resonance, int sampleRate)
        => new(FilterType.Lowpass, cutoff, resonance, sampleRate);

    /// <summary>Construct a highpass filter.</summary>
    public static BiquadFilter Highpass(float cutoff, float resonance, int sampleRate)
        => new(FilterType.Highpass, cutoff, resonance, sampleRate);

    /// <summary>Construct a bandpass filter.</summary>
    public static BiquadFilter Bandpass(float cutoff, float resonance, int sampleRate)
        => new(FilterType.Bandpass, cutoff, resonance, sampleRate);

    public BiquadFilter(FilterType type, float cutoff, float resonance, int sampleRate)
    {
        _type       = type;
        _cutoff     = cutoff;
        _resonance  = resonance;
        _sampleRate = sampleRate;
        UpdateCoefficients();
    }

    /// <summary>Process one sample. Input and output are in -1..+1 range.</summary>
    public float Process(float x)
    {
        float y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
        _x2 = _x1; _x1 = x;
        _y2 = _y1; _y1 = y;
        return y;
    }

    /// <summary>Reset filter state (clear delay line).</summary>
    public void Reset() => _x1 = _x2 = _y1 = _y2 = 0f;

    private void UpdateCoefficients()
    {
        // Standard bilinear transform biquad design (Audio EQ Cookbook — R. Bristow-Johnson)
        float w0 = MathF.Tau * Math.Clamp(_cutoff, 1f, _sampleRate / 2f - 1f) / _sampleRate;
        float cosW0 = MathF.Cos(w0);
        float sinW0 = MathF.Sin(w0);
        float q  = Math.Max(0.01f, _resonance);
        float alpha = sinW0 / (2f * q);

        float a0;
        switch (_type)
        {
            case FilterType.Lowpass:
                _b0 = (1f - cosW0) / 2f;
                _b1 =  1f - cosW0;
                _b2 = (1f - cosW0) / 2f;
                a0  =  1f + alpha;
                _a1 = -2f * cosW0;
                _a2 =  1f - alpha;
                break;

            case FilterType.Highpass:
                _b0 =  (1f + cosW0) / 2f;
                _b1 = -(1f + cosW0);
                _b2 =  (1f + cosW0) / 2f;
                a0  =   1f + alpha;
                _a1 = -2f * cosW0;
                _a2 =  1f - alpha;
                break;

            case FilterType.Bandpass:
                _b0 =  sinW0 / 2f;
                _b1 =  0f;
                _b2 = -sinW0 / 2f;
                a0  =  1f + alpha;
                _a1 = -2f * cosW0;
                _a2 =  1f - alpha;
                break;

            default:
                _b0 = 1f; _b1 = _b2 = _a1 = _a2 = 0f;
                return;
        }

        // Normalize by a0
        _b0 /= a0; _b1 /= a0; _b2 /= a0;
        _a1 /= a0; _a2 /= a0;
    }
}

/// <summary>Convenience alias — lowpass filter.</summary>
public sealed class LowpassFilter : BiquadFilter
{
    public LowpassFilter(float cutoff, float resonance, int sampleRate)
        : base(FilterType.Lowpass, cutoff, resonance, sampleRate) { }
}

/// <summary>Convenience alias — highpass filter.</summary>
public sealed class HighpassFilter : BiquadFilter
{
    public HighpassFilter(float cutoff, float resonance, int sampleRate)
        : base(FilterType.Highpass, cutoff, resonance, sampleRate) { }
}
