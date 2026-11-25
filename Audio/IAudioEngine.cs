using DvorFight.Core;

namespace DvorFight.Audio;

/// <summary>Abstraction over the audio backend so gameplay can run without a device
/// (dependency inversion; also lets the headless self-test stub audio).</summary>
public interface IAudioEngine
{
    void Play(SoundType type);
}
