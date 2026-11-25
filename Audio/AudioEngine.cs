using DvorFight.Core;
using Microsoft.Xna.Framework.Audio;

namespace DvorFight.Audio;

/// <summary>
/// Generates every sound effect procedurally at startup — sine / triangle / saw
/// oscillators plus filtered-sounding noise, with frequency sweeps and an envelope.
/// No audio files are shipped, mirroring the original WebAudio approach.
/// </summary>
public sealed class AudioEngine : IAudioEngine
{
    private const int SampleRate = 44100;
    private const float TwoPi = MathF.PI * 2f;
    private const float Volume = 0.8f;

    private readonly Dictionary<SoundType, SoundEffect> _effects = new();

    public void Load()
    {
        _effects[SoundType.Swing] = Make((Voice.Noise, 2100f, 800f, 0.12f, 0.18f));
        _effects[SoundType.Beam] = Make((Voice.Triangle, 980f, 170f, 0.22f, 0.16f));
        _effects[SoundType.Form] = Make((Voice.Saw, 42f, 122f, 0.38f, 0.10f));
        _effects[SoundType.Summon] = Make((Voice.Sine, 330f, 660f, 0.22f, 0.14f));
        _effects[SoundType.Impact] = Make((Voice.Sine, 195f, 65f, 0.14f, 0.32f), (Voice.Noise, 3000f, 390f, 0.09f, 0.34f));
        _effects[SoundType.Heavy] = Make((Voice.Sine, 142f, 36f, 0.28f, 0.55f), (Voice.Noise, 1650f, 390f, 0.17f, 0.34f));
    }

    public void Play(SoundType type)
    {
        if (_effects.TryGetValue(type, out var effect))
            effect.Play(Volume, 0f, 0f);
    }

    private enum Voice
    {
        Sine,
        Triangle,
        Saw,
        Noise,
    }

    private static SoundEffect Make(params (Voice kind, float f0, float f1, float dur, float vol)[] voices)
    {
        int total = 0;
        foreach (var voice in voices)
            total = Math.Max(total, (int)(voice.dur * SampleRate));

        var buffer = new float[total];
        foreach (var voice in voices)
        {
            int n = (int)(voice.dur * SampleRate);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                // Integrated frequency sweep: phase(f) integrates f0 -> f1 linearly.
                float phase = voice.f0 * t + (voice.f1 - voice.f0) * t * t / (2f * voice.dur);
                float sample = voice.kind switch
                {
                    Voice.Sine => MathF.Sin(TwoPi * phase),
                    Voice.Triangle => Triangle(phase),
                    Voice.Saw => Saw(phase),
                    _ => Random.Shared.NextSingle() * 2f - 1f,
                };
                buffer[i] += sample * Envelope(t, voice.dur) * voice.vol;
            }
        }

        return ToEffect(buffer);
    }

    private static float Envelope(float t, float duration)
    {
        float attack = Math.Min(1f, t / 0.005f);
        float decay = MathF.Exp(-6f * t / duration);
        return attack * decay;
    }

    private static float Triangle(float phase)
    {
        float p = phase - MathF.Floor(phase);
        return p < 0.5f ? 4f * p - 1f : 3f - 4f * p;
    }

    private static float Saw(float phase)
    {
        float p = phase - MathF.Floor(phase);
        return 2f * p - 1f;
    }

    private static SoundEffect ToEffect(float[] buffer)
    {
        var samples = new short[buffer.Length];
        for (int i = 0; i < buffer.Length; i++)
        {
            float clamped = Math.Clamp(buffer[i], -1f, 1f);
            samples[i] = (short)(clamped * short.MaxValue);
        }

        var bytes = new byte[buffer.Length * 2];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return new SoundEffect(bytes, SampleRate, AudioChannels.Mono);
    }
}
