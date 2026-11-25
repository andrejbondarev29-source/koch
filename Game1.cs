using DvorFight.Audio;
using DvorFight.Combat;
using DvorFight.Config;
using DvorFight.Core;
using DvorFight.Domain;
using DvorFight.Input;
using DvorFight.Rendering;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DvorFight;

/// <summary>
/// The MonoGame shell: window, game loop, menu / fight / result screens.
/// All gameplay lives in <see cref="FightSession"/> and the domain layer.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    private FontSystem _fontSystem = null!;
    private DynamicSpriteFont _fontTitle = null!;
    private DynamicSpriteFont _fontBig = null!;
    private DynamicSpriteFont _fontName = null!;
    private DynamicSpriteFont _fontSmall = null!;

    private AssetStore _assets = null!;
    private Renderer _renderer = null!;
    private readonly ScreenScaler _scaler = new();
    private Background _background = null!;
    private Hud _hud = null!;
    private AudioEngine _audio = null!;
    private InputController _input = null!;
    private FightSession _session = null!;

    private GameScreen _screen = GameScreen.Menu;
    private KeyboardState _prevKeyboard;
    private MouseState _prevMouse;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = Balance.ViewWidth;
        _graphics.PreferredBackBufferHeight = Balance.ViewHeight;
        _graphics.ApplyChanges();
        Window.Title = "Дворовой файтинг";
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        string fontDir = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts");
        string fontPath = File.Exists(Path.Combine(fontDir, "segoeui.ttf"))
            ? Path.Combine(fontDir, "segoeui.ttf")
            : Path.Combine(fontDir, "arial.ttf");

        _fontSystem = new FontSystem(new FontSystemSettings());
        _fontSystem.AddFont(File.ReadAllBytes(fontPath));
        _fontTitle = _fontSystem.GetFont(48);
        _fontBig = _fontSystem.GetFont(24);
        _fontName = _fontSystem.GetFont(19);
        _fontSmall = _fontSystem.GetFont(15);

        _assets = new AssetStore(GraphicsDevice);
        _assets.Load();

        _renderer = new Renderer(GraphicsDevice, _assets);
        _background = new Background(GraphicsDevice);
        _hud = new Hud();

        _audio = new AudioEngine();
        _audio.Load();

        _input = new InputController();
        _session = new FightSession(_audio, _assets.LoadCutsceneFrames());
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = Math.Min(0.035f, (float)gameTime.ElapsedGameTime.TotalSeconds);
        _scaler.Update(Window.ClientBounds);

        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        _input.Update(keyboard, mouse);

        if (_screen == GameScreen.Menu)
            HandleMenu(keyboard, mouse);
        else
            HandleFight(dt, keyboard, mouse);

        _prevKeyboard = keyboard;
        _prevMouse = mouse;
        base.Update(gameTime);
    }

    private void HandleMenu(KeyboardState keyboard, MouseState mouse)
    {
        if (Click(mouse))
        {
            var point = _scaler.ToVirtual(mouse.Position);
            StartFight(point.X < Balance.ViewWidth / 2f ? FighterKind.Andrey : FighterKind.Pantera);
            return;
        }

        if (Pressed(keyboard, Keys.D1) || Pressed(keyboard, Keys.NumPad1))
            StartFight(FighterKind.Andrey);
        else if (Pressed(keyboard, Keys.D2) || Pressed(keyboard, Keys.NumPad2))
            StartFight(FighterKind.Pantera);
    }

    private void HandleFight(float dt, KeyboardState keyboard, MouseState mouse)
    {
        foreach (var command in _input.ConsumeCommands())
            _session.Player.IssueCommand(command, _session);

        _session.Update(dt, _input);

        if (!_session.Playing)
        {
            if (Pressed(keyboard, Keys.R) || Pressed(keyboard, Keys.Enter) || Click(mouse))
                _session.Start(_session.Player.Kind);
            else if (Pressed(keyboard, Keys.Escape))
                _screen = GameScreen.Menu;
        }
        else if (Pressed(keyboard, Keys.Escape))
        {
            _screen = GameScreen.Menu;
        }
    }

    private void StartFight(FighterKind kind)
    {
        _ = _input.ConsumeCommands(); // discard any stale command queued in the menu
        _session.Start(kind);
        _screen = GameScreen.Fight;
    }

    private bool Click(MouseState mouse) =>
        mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && _prevKeyboard.IsKeyUp(key);

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        float ox = 0f, oy = 0f;
        if (_screen == GameScreen.Fight)
        {
            ox = MathF.Sin(_session.Clock * 127f) * _session.Shake;
            oy = MathF.Cos(_session.Clock * 93f) * _session.Shake * 0.55f;
        }
        var matrix = Matrix.CreateTranslation(ox, oy, 0f) * _scaler.Matrix;

        _spriteBatch.Begin(
            blendState: BlendState.NonPremultiplied,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: matrix);
        _background.Draw(_spriteBatch);

        if (_screen == GameScreen.Fight)
        {
            _renderer.Clock = _session.Clock;
            _session.Draw(_spriteBatch, _renderer);
        }
        else
        {
            DrawMenu(_spriteBatch, (float)gameTime.TotalGameTime.TotalSeconds);
        }
        _spriteBatch.End();

        // HUD and UI are drawn without screen shake (but still scaled to the window).
        _spriteBatch.Begin(
            blendState: BlendState.NonPremultiplied,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: matrix);

        if (_screen == GameScreen.Fight && !_session.InCutscene)
        {
            _hud.Draw(_spriteBatch, _renderer, _fontName, _fontSmall, _session);
            if (!_session.Playing)
                DrawResult(_spriteBatch);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawMenu(SpriteBatch spriteBatch, float time)
    {
        // Dim the arena behind the cards so the menu reads clearly.
        _renderer.FillRect(spriteBatch,
            new Rectangle(0, 0, Balance.ViewWidth, Balance.ViewHeight),
            new Color(0x0b, 0x0d, 0x15) * 0.74f);

        UI.ShadowedText(spriteBatch, _fontTitle, "ДВОРОВОЙ ФАЙТИНГ",
            new Vector2(Balance.ViewWidth / 2f, 52), new Color(0xff, 0xf4, 0xdd), TextAlign.Center);
        UI.Text(spriteBatch, _fontName, "ВЫБЕРИ БОЙЦА",
            new Vector2(Balance.ViewWidth / 2f, 96), new Color(0xff, 0xcf, 0x53), TextAlign.Center);

        var mouse = _scaler.ToVirtual(Mouse.GetState().Position);
        float pulse = 0.5f + 0.5f * MathF.Sin(time * 4f);

        var leftCard = new Rectangle(130, 112, 330, 290);
        var rightCard = new Rectangle(500, 112, 330, 290);

        DrawFighterCard(spriteBatch, leftCard, "andrey/walk", "Андрей", "Вонючие ноги · Метаморфоза", IsHover(leftCard, mouse), pulse);
        DrawFighterCard(spriteBatch, rightCard, "pantera/walk", "Пантера", "Ушной луч · Призыв", IsHover(rightCard, mouse), pulse);

        UI.Text(spriteBatch, _fontSmall, "1 / клик по карточке — выбрать бойца",
            new Vector2(Balance.ViewWidth / 2f, 432), new Color(0xd1, 0xce, 0xd9), TextAlign.Center);
        UI.Text(spriteBatch, _fontSmall, "A/D — шаг · W — прыжок · S — присед · ЛКМ — удар · Z/X — спецухи",
            new Vector2(Balance.ViewWidth / 2f, 456), new Color(0x8f, 0x8c, 0x9d), TextAlign.Center);
    }

    private void DrawFighterCard(
        SpriteBatch spriteBatch,
        Rectangle card,
        string animationKey,
        string name,
        string description,
        bool hovered,
        float pulse)
    {
        var fill = hovered ? new Color(0x28, 0x2d, 0x3f) : new Color(0x1d, 0x21, 0x30);
        var border = hovered
            ? Color.Lerp(new Color(0xff, 0xcf, 0x53), Color.White, pulse * 0.35f)
            : new Color(0x5a, 0x5c, 0x6e);

        _renderer.FillRect(spriteBatch, card, fill);
        _renderer.DrawBorder(spriteBatch, card, border, hovered ? 3 : 2);

        float centerX = card.X + card.Width / 2f;
        float feetY = card.Y + card.Height - 82f;
        _renderer.DrawSprite(spriteBatch, animationKey, 0, centerX, feetY, SpriteEffects.None, 1f, 185f);

        var nameColor = hovered ? new Color(0xff, 0xcf, 0x53) : Color.White;
        UI.Text(spriteBatch, _fontBig, name, new Vector2(centerX, card.Y + card.Height - 56), nameColor, TextAlign.Center);
        UI.Text(spriteBatch, _fontSmall, description, new Vector2(centerX, card.Y + card.Height - 26),
            new Color(0xd1, 0xce, 0xd9), TextAlign.Center);
    }

    private static bool IsHover(Rectangle rectangle, Vector2 point) =>
        point.X >= rectangle.X && point.X < rectangle.Right
        && point.Y >= rectangle.Y && point.Y < rectangle.Bottom;

    private void DrawResult(SpriteBatch spriteBatch)
    {
        bool win = _session.Winner != null && ReferenceEquals(_session.Winner, _session.Player);
        string text = _session.Winner == null ? "НИЧЬЯ" : win ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
        var color = _session.Winner == null
            ? new Color(0xd1, 0xce, 0xd9)
            : win ? new Color(0xff, 0xd9, 0x6c) : new Color(0xfc, 0x89, 0x78);

        _renderer.FillRect(spriteBatch,
            new Rectangle(0, 0, Balance.ViewWidth, Balance.ViewHeight),
            new Color(0x08, 0x0a, 0x12) * 0.62f);

        var panel = new Rectangle(200, 150, 560, 170);
        _renderer.DrawPanel(spriteBatch, panel, new Color(0x17, 0x1a, 0x26) * 0.97f, new Color(0x64, 0x65, 0x78), 3);

        UI.ShadowedText(spriteBatch, _fontTitle, text, new Vector2(Balance.ViewWidth / 2f, 208), color, TextAlign.Center);
        UI.Text(spriteBatch, _fontSmall, "R / Enter / клик — Реванш      Esc — Выбор бойца",
            new Vector2(Balance.ViewWidth / 2f, 278), Color.White, TextAlign.Center);
    }
}
