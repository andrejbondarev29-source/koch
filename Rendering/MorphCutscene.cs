using DvorFight.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Plays the extracted metamorphosis animation (the original MP4 cutscene,
/// converted to PNG frames) full-screen. Timer-driven, so it can never soft-lock.
/// </summary>
public sealed class MorphCutscene
{
    private const float FramesPerSecond = 12f;

    private readonly Texture2D[] _frames;
    private float _elapsed;

    public MorphCutscene(Texture2D[] frames)
    {
        _frames = frames;
    }

    public bool Finished => _elapsed >= _frames.Length / FramesPerSecond;

    public void Update(float dt) => _elapsed += dt;

    public void Draw(SpriteBatch spriteBatch)
    {
        int index = Math.Clamp((int)(_elapsed * FramesPerSecond), 0, _frames.Length - 1);
        spriteBatch.Draw(_frames[index], new Rectangle(0, 0, Balance.ViewWidth, Balance.ViewHeight), Color.White);
    }
}
