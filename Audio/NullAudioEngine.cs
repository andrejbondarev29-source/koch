using DvorFight.Core;

namespace DvorFight.Audio;

/// <summary>Silent audio engine used by headless tests.</summary>
public sealed class NullAudioEngine : IAudioEngine
{
    public void Play(SoundType type) { }
}
