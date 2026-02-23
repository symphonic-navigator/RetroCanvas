# Audio Synthesis

## Architecture

All synthesis is **per-sample** and **composable**:

```
Oscillator → Filter → Envelope → short
```

Each component produces or consumes one `float` (-1..+1) per call. Chain them freely.

## Oscillator

```csharp
var osc = new Oscillator(Waveform.Sawtooth, frequency: 440f, sampleRate: 22050);
osc.SetMidiNote(69);  // A4 = 440Hz
float s = osc.Next(); // -1..+1
```

Waveforms: `Sine`, `Square`, `Sawtooth`, `Triangle`, `Noise`

## Filters (biquad IIR)

```csharp
var lp = new LowpassFilter(cutoff: 1200f, resonance: 0.7f, sampleRate: 22050);
var hp = new HighpassFilter(cutoff: 200f,  resonance: 0.5f, sampleRate: 22050);

// Dynamic filter sweep
lp.Cutoff = 200f + MathF.Sin(t) * 1000f;

float filtered = lp.Process(raw);
```

Internally: Audio EQ Cookbook (R. Bristow-Johnson) bilinear-transform biquad.

## ADSR Envelope

```csharp
var env = new AdsrEnvelope(
    attack:  0.01f,  // seconds
    decay:   0.1f,
    sustain: 0.6f,   // amplitude 0..1
    release: 0.3f,
    sampleRate: 22050);

env.NoteOn();   // trigger attack
env.NoteOff();  // trigger release
float amp = env.Next(); // 0..1
```

## Full voice example

```csharp
var osc    = new Oscillator(Waveform.Sawtooth, 220f, 22050);
var filter = new LowpassFilter(400f, 0.8f, 22050);
var env    = new AdsrEnvelope(0.005f, 0.15f, 0.5f, 0.4f, 22050);

short NextSample()
{
    float s = osc.Next();
    s = filter.Process(s);
    s *= env.Next();
    return (short)(s * 32767f);
}
```

## AudioDriver callback

```csharp
var audio = new AudioDriver(new AudioConfig
{
    SampleRate = 22050,
    BitDepth   = BitDepth.Signed16,
    Channels   = ChannelCount.Mono,
    BufferSize = 512,
});

audio.OnBufferNeeded += (Span<short> buffer) =>
{
    for (int i = 0; i < buffer.Length; i++)
        buffer[i] = NextSample();
};

audio.Start();
```

The callback runs on SDL's audio thread. Keep it allocation-free and lock-free.
