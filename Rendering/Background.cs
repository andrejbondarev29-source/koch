using DvorFight.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// The static arena backdrop (sky, sun, skyline, ground). Generated once into a
/// texture, so it is drawn with a single blit per frame instead of ~150 rects.
/// </summary>
public sealed class Background
{
    private static readonly Color SkyTop = new(0x51, 0x47, 0x66);
    private static readonly Color SkyBottom = new(0xe9, 0xa3, 0x7f);
    private static readonly Color Sun = new(0xff, 0xdb, 0x9c);
    private static readonly Color BuildingA = new(0x56, 0x50, 0x65);
    private static readonly Color BuildingB = new(0x49, 0x46, 0x5d);
    private static readonly Color Window = new(0xce, 0xaa, 0x87);
    private static readonly Color Ground = new(0x38, 0x39, 0x47);
    private static readonly Color FloorLine = new(0xe4, 0xb6, 0x86);

    private readonly Texture2D _texture;

    public Background(GraphicsDevice graphicsDevice)
    {
        _texture = Generate(graphicsDevice);
    }

    public void Draw(SpriteBatch spriteBatch) =>
        spriteBatch.Draw(_texture, Vector2.Zero, Color.White);

    private static Texture2D Generate(GraphicsDevice graphicsDevice)
    {
        var colors = new Color[Balance.ViewWidth * Balance.ViewHeight];

        // Sky gradient (0 .. floor).
        for (int y = 0; y < Balance.ViewHeight; y++)
        {
            float t = Math.Clamp(y / Balance.Floor, 0f, 1f);
            Color sky = Color.Lerp(SkyTop, SkyBottom, t);
            int baseIndex = y * Balance.ViewWidth;
            for (int x = 0; x < Balance.ViewWidth; x++)
                colors[baseIndex + x] = sky;
        }

        FillCircle(colors, 741, 130, 35, Sun);

        // Skyline: 16 buildings with windows.
        for (int j = 0; j < 16; j++)
        {
            int height = 55 + (j * 47) % 105;
            Color building = j % 2 == 0 ? BuildingA : BuildingB;
            int bx = j * 68;

            for (int y = 335 - height; y < 335; y++)
                for (int x = bx; x < bx + 62; x++)
                    Set(colors, x, y, building);

            for (int k = 0; k < 4; k++)
                for (int m = 0; m < 3; m++)
                    if ((j + k + m) % 3 != 0)
                        for (int y = 345 - height + m * 16; y < 345 - height + m * 16 + 8; y++)
                            for (int x = bx + 7 + k * 13; x < bx + 7 + k * 13 + 6; x++)
                                Set(colors, x, y, Window);
        }

        // Ground slab.
        for (int y = 330; y < Balance.ViewHeight; y++)
            for (int x = 0; x < Balance.ViewWidth; x++)
                Set(colors, x, y, Ground);

        // Floor contact line.
        for (int x = 0; x < Balance.ViewWidth; x++)
        {
            Set(colors, x, 405, FloorLine);
            Set(colors, x, 406, FloorLine);
        }

        var texture = new Texture2D(graphicsDevice, Balance.ViewWidth, Balance.ViewHeight);
        texture.SetData(colors);
        return texture;
    }

    private static void FillCircle(Color[] colors, int cx, int cy, int radius, Color color)
    {
        for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                    Set(colors, x, y, color);
    }

    private static void Set(Color[] colors, int x, int y, Color color)
    {
        if (x < 0 || x >= Balance.ViewWidth || y < 0 || y >= Balance.ViewHeight)
            return;
        colors[y * Balance.ViewWidth + x] = color;
    }
}
