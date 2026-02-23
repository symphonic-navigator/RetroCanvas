namespace RetroCanvas.Core.Audio.Synthesis;

/// <summary>ADSR envelope states.</summary>
public enum EnvelopeState
{
    Idle,
    Attack,
    Decay,
    Sustain,
    Release,
}

/// <summary>
/// ADSR (Attack-Decay-Sustain-Release) amplitude envelope.
/// Call <see cref="Next"/> once per sample; multiply by oscillator output.
/// All time values are in seconds.
/// </summary>
public sealed class AdsrEnvelope
{
    private readonly int _sampleRate;

    private EnvelopeState _state = EnvelopeState.Idle;
    private float _value;
    private float _releaseStart;

    /// <summary>Attack time in seconds (0 → instant, 1 → 1 second ramp up).</summary>
    public float Attack  { get; set; }

    /// <summary>Decay time in seconds (time to fall from peak to sustain level).</summary>
    public float Decay   { get; set; }

    /// <summary>Sustain level 0..1 (amplitude held while key is down).</summary>
    public float Sustain { get; set; }

    /// <summary>Release time in seconds (time to fall to 0 after note-off).</summary>
    public float Release { get; set; }

    public EnvelopeState State => _state;
    public float         Value => _value;

    public AdsrEnvelope(float attack, float decay, float sustain, float release, int sampleRate)
    {
        Attack     = attack;
        Decay      = decay;
        Sustain    = sustain;
        Release    = release;
        _sampleRate = sampleRate;
    }

    /// <summary>Trigger note-on — starts the attack phase.</summary>
    public void NoteOn()
    {
        _state = EnvelopeState.Attack;
        // If retriggered during release, start from current value for click-free retrigger
    }

    /// <summary>Trigger note-off — starts the release phase.</summary>
    public void NoteOff()
    {
        if (_state != EnvelopeState.Idle)
        {
            _releaseStart = _value;
            _state        = EnvelopeState.Release;
        }
    }

    /// <summary>Compute one sample of the envelope (0..1). Advance state machine.</summary>
    public float Next()
    {
        float step = 1f / _sampleRate;

        switch (_state)
        {
            case EnvelopeState.Idle:
                _value = 0f;
                break;

            case EnvelopeState.Attack:
                if (Attack <= 0f)
                {
                    _value = 1f;
                    _state = EnvelopeState.Decay;
                }
                else
                {
                    _value = Math.Min(1f, _value + step / Attack);
                    if (_value >= 1f) _state = EnvelopeState.Decay;
                }
                break;

            case EnvelopeState.Decay:
                if (Decay <= 0f)
                {
                    _value = Sustain;
                    _state = EnvelopeState.Sustain;
                }
                else
                {
                    _value = Math.Max(Sustain, _value - step / Decay);
                    if (_value <= Sustain) _state = EnvelopeState.Sustain;
                }
                break;

            case EnvelopeState.Sustain:
                _value = Sustain;
                break;

            case EnvelopeState.Release:
                if (Release <= 0f)
                {
                    _value = 0f;
                    _state = EnvelopeState.Idle;
                }
                else
                {
                    _value = Math.Max(0f, _value - _releaseStart * step / Release);
                    if (_value <= 0f) { _value = 0f; _state = EnvelopeState.Idle; }
                }
                break;
        }

        return _value;
    }

    /// <summary>Hard reset to idle state.</summary>
    public void Reset() { _value = 0f; _state = EnvelopeState.Idle; }
}
