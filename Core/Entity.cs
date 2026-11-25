using DvorFight.Rendering;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Core;

/// <summary>
/// Root of every drawable thing in the world. Provides shared spatial state
/// (X, Y, lifetime) and a polymorphic <see cref="Draw"/> used by the renderer.
/// </summary>
public abstract class Entity
{
    public float X { get; set; }
    public float Y { get; set; }
    public bool Dead { get; set; }
    public float Age { get; set; }

    public abstract void Draw(SpriteBatch spriteBatch, Renderer renderer);
}
