using System.Reflection;
using DvorFight.Audio;
using DvorFight.Combat;
using DvorFight.Config;
using DvorFight.Core;
using DvorFight.Input;
using DvorFight.Rendering;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DvorFight;

/// <summary>
/// The MonoGame shell: window, game loop and transitions between the menu screens and
/// the fight. All gameplay lives in <see cref="FightSession"/>; all menu logic and
/// drawing lives in the <see cref="MenuScreen"/> subclasses under Rendering/.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    private FontSystem _fontSystem = null!;
    private FontSystem? _displayFontSystem;
    private FontSet _fonts = null!;

    private AssetStore _assets = null!;
    private Renderer _renderer = null!;
    private readonly ScreenScaler _scaler = new();
    private Background _background = null!;
    private Hud _hud = null!;
    private ResultOverlay _resultOverlay = null!;
    private AudioEngine _audio = null!;
    private InputController _input = null!;
    private readonly InputController _neutralInput = new();
    private FightSession _session = null!;

    private readonly MenuState _menuState = new();
    private MenuScreen _menuScreen = null!;

    private GameScreen _screen = GameScreen.Title;
    private GameScreen _lastSelectScreen = GameScreen.FighterMenu;
    private KeyboardState _prevKeyboard;
    private MouseState _prevMouse;
    private int _framesDrawn;

    // Swallow the second press of a double-click that opened a new menu screen, so a
    // double-click on «Играть» / «Бойцы» cannot click straight through to "start fight".
    private readonly MenuClickGuard _menuClickGuard = new();

    /// <summary>When set, the game renders a few frames, saves a PNG and exits (dev aid).</summary>
    public string? ScreenshotPath { get; set; }

    /// <summary>Screen the game opens on (dev aid for screenshots: title / fighters / fight).</summary>
    public GameScreen InitialScreen { get; set; } = GameScreen.Title;

    /// <summary>Fighter preselected in the menu (dev aid for screenshots).</summary>
    public FighterKind InitialFighter { get; set; } = FighterKind.Andrey;

    /// <summary>Difficulty preselected in the menu (dev aid for screenshots).</summary>
    public BotDifficulty InitialDifficulty { get; set; } = BotDifficulty.Normal;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    private static string GetProductVersion()
    {
        string version = typeof(Game1).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.01";
        int metadataSeparator = version.IndexOf('+');
        return metadataSeparator >= 0 ? version[..metadataSeparator] : version;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = Balance.ViewWidth;
        _graphics.PreferredBackBufferHeight = Balance.ViewHeight;
        _graphics.ApplyChanges();
        Window.Title = $"Дворовой файтинг {GetProductVersion()}";
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        string fontDir = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts");
        string fontPath = File.Exists(Path.Combine(fontDir, "segoeuib.ttf"))
            ? Path.Combine(fontDir, "segoeuib.ttf")
            : File.Exists(Path.Combine(fontDir, "segoeui.ttf"))
                ? Path.Combine(fontDir, "segoeui.ttf")
                : Path.Combine(fontDir, "arial.ttf");

        // PremultiplyAlpha=false keeps the glyph atlas in straight alpha, matching the
        // NonPremultiplied blend state used for the straight-alpha PNG sprites. Without
        // this the text is multiplied by alpha twice and looks washed out / faded.
        var fontSettings = new FontSystemSettings
        {
            PremultiplyAlpha = false,
        };
        _fontSystem = new FontSystem(fontSettings);
        _fontSystem.AddFont(File.ReadAllBytes(fontPath));

        // Optional geometric display face for the fighter-select hero name. When the file
        // is absent the regular UI font is used at the same size, so no asset is required.
        string displayPath = Path.Combine(fontDir, "display.ttf");
        if (File.Exists(displayPath))
        {
            _displayFontSystem = new FontSystem(fontSettings);
            _displayFontSystem.AddFont(File.ReadAllBytes(displayPath));
        }

        FontSystem displaySystem = _displayFontSystem ?? _fontSystem;
        _fonts = new FontSet(
            _fontSystem.GetFont(48),
            _fontSystem.GetFont(24),
            _fontSystem.GetFont(19),
            _fontSystem.GetFont(15),
            displaySystem.GetFont(64));

        _assets = new AssetStore(GraphicsDevice);
        _assets.Load();

        _renderer = new Renderer(GraphicsDevice, _assets);
        _background = new Background(GraphicsDevice);
        _hud = new Hud();
        _resultOverlay = new ResultOverlay();

        _audio = new AudioEngine();
        _audio.Load();

        _input = new InputController();
        _session = new FightSession(_audio, _assets.LoadCutsceneFrames());

        // Avoid a phantom edge-trigger / hover on the very first frame: the default
        // KeyboardState/MouseState are "empty" and differ from the real devices.
        _prevKeyboard = Keyboard.GetState();
        _prevMouse = Mouse.GetState();

        _menuState.SelectedFighter = InitialFighter;
        _menuState.Difficulty = InitialDifficulty;

        _screen = InitialScreen;
        _lastSelectScreen = _screen == GameScreen.FighterSelect
            ? GameScreen.FighterSelect
            : GameScreen.FighterMenu;
        switch (_screen)
        {
            case GameScreen.Fight:
                _session.Start(_menuState.SelectedFighter, _menuState.Difficulty);
                _menuScreen = CreateFighterMenu();
                break;
            case GameScreen.FighterMenu:
                _menuScreen = CreateFighterMenu();
                break;
            case GameScreen.FighterSelect:
                _menuScreen = CreateFighterSelect();
                break;
            default:
                _screen = GameScreen.Title;
                _menuScreen = new TitleScreen();
                break;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = Math.Min(0.035f, (float)gameTime.ElapsedGameTime.TotalSeconds);
        _scaler.Update(Window.ClientBounds);

        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        _input.Update(keyboard, mouse);

        if (_screen == GameScreen.Fight)
            HandleFight(dt, keyboard, mouse);
        else
            HandleMenu(dt, keyboard, mouse, gameTime.TotalGameTime.TotalSeconds);

        _prevKeyboard = keyboard;
        _prevMouse = mouse;
        base.Update(gameTime);
    }

    private void HandleMenu(float dt, KeyboardState keyboard, MouseState mouse, double now)
    {
        var input = new MenuInput
        {
            Up = Pressed(keyboard, Keys.Up) || Pressed(keyboard, Keys.W),
            Down = Pressed(keyboard, Keys.Down) || Pressed(keyboard, Keys.S),
            Left = Pressed(keyboard, Keys.Left) || Pressed(keyboard, Keys.A),
            Right = Pressed(keyboard, Keys.Right) || Pressed(keyboard, Keys.D),
            Confirm = Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space),
            Back = Pressed(keyboard, Keys.Escape),
            MousePosition = _scaler.ToVirtual(mouse.Position),
            MouseMoved = mouse.Position != _prevMouse.Position,
            MouseClicked = Click(mouse),
            FighterShortcut =
                Pressed(keyboard, Keys.D1) || Pressed(keyboard, Keys.NumPad1) ? 1
                : Pressed(keyboard, Keys.D2) || Pressed(keyboard, Keys.NumPad2) ? 2
                : 0,
        };

        // Screenshot mode must be deterministic: ignore the real mouse so a stray click
        // that activates the window cannot start a fight before the PNG is captured.
        if (ScreenshotPath != null)
            input = new MenuInput { MousePosition = new Vector2(-100f, -100f) };

        if (_menuClickGuard.ShouldSuppress(now, input))
            input = input.WithoutMouseClick();

        var command = _menuScreen.Update(input, dt);
        switch (command.Type)
        {
            case MenuCommandType.Play:
                OpenFighterMenu();
                if (input.MouseClicked)
                    _menuClickGuard.Arm(now, input.MousePosition);
                break;
            case MenuCommandType.OpenFighters:
                OpenFighterSelect();
                if (input.MouseClicked)
                    _menuClickGuard.Arm(now, input.MousePosition);
                break;
            case MenuCommandType.StartFight:
                StartFight(command.Fighter, command.Difficulty);
                break;
            case MenuCommandType.BackToTitle:
                GoToTitle();
                break;
            case MenuCommandType.Exit:
                Exit();
                break;
        }
    }

    private void HandleFight(float dt, KeyboardState keyboard, MouseState mouse)
    {
        if (ScreenshotPath == null)
        {
            foreach (var command in _input.ConsumeCommands())
                _session.Player.IssueCommand(command, _session);
        }

        _session.Update(dt, ScreenshotPath != null ? _neutralInput : _input);

        // Keep the fight screenshot deterministic: no stray Esc/rematch/attack input.
        if (ScreenshotPath != null)
            return;

        if (!_session.Playing)
        {
            if (Pressed(keyboard, Keys.R) || Pressed(keyboard, Keys.Enter) || Click(mouse))
                StartFight(_session.Player.Kind, _session.Difficulty);
            else if (Pressed(keyboard, Keys.Escape))
                OpenLastSelect();
        }
        else if (Pressed(keyboard, Keys.Escape))
        {
            OpenLastSelect();
        }
    }

    private FighterMenuScreen CreateFighterMenu() =>
        new(_menuState);

    private FighterSelectScreen CreateFighterSelect() =>
        new(_menuState);

    private void OpenFighterMenu()
    {
        _ = _input.ConsumeCommands(); // drop anything queued while a menu was open
        _menuScreen = CreateFighterMenu();
        _screen = GameScreen.FighterMenu;
        _lastSelectScreen = _screen;
    }

    private void OpenFighterSelect()
    {
        _ = _input.ConsumeCommands(); // drop anything queued while a menu was open
        _menuScreen = CreateFighterSelect();
        _screen = GameScreen.FighterSelect;
        _lastSelectScreen = _screen;
    }

    private void OpenLastSelect()
    {
        if (_lastSelectScreen == GameScreen.FighterSelect)
            OpenFighterSelect();
        else
            OpenFighterMenu();
    }

    private void GoToTitle()
    {
        _ = _input.ConsumeCommands();
        _menuScreen = new TitleScreen();
        _screen = GameScreen.Title;
    }

    private void StartFight(FighterKind kind, BotDifficulty difficulty)
    {
        _ = _input.ConsumeCommands(); // discard stale menu commands
        _menuState.SelectedFighter = kind;
        _menuState.Difficulty = difficulty;
        if (_screen == GameScreen.FighterMenu || _screen == GameScreen.FighterSelect)
            _lastSelectScreen = _screen;
        _session.Start(kind, difficulty);
        _screen = GameScreen.Fight;
    }

    private bool Click(MouseState mouse) =>
        mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && _prevKeyboard.IsKeyUp(key);

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        float time = (float)gameTime.TotalGameTime.TotalSeconds;

        // World layer: arena + fight (with screen shake), or the menu screens.
        float ox = 0f, oy = 0f;
        if (_screen == GameScreen.Fight)
        {
            ox = MathF.Sin(_session.Clock * 127f) * _session.Shake;
            oy = MathF.Cos(_session.Clock * 93f) * _session.Shake * 0.55f;
        }
        var worldMatrix = Matrix.CreateTranslation(ox, oy, 0f) * _scaler.Matrix;

        _spriteBatch.Begin(
            blendState: BlendState.NonPremultiplied,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: worldMatrix);
        _background.Draw(_spriteBatch);

        _renderer.Clock = _screen == GameScreen.Fight ? _session.Clock : time;
        if (_screen == GameScreen.Fight)
            _session.Draw(_spriteBatch, _renderer);
        else
            _menuScreen.Draw(_spriteBatch, _renderer, _fonts, time);
        _spriteBatch.End();

        // HUD and result stay stable while the world shakes.
        if (_screen == GameScreen.Fight)
        {
            _spriteBatch.Begin(
                blendState: BlendState.NonPremultiplied,
                samplerState: SamplerState.LinearClamp,
                transformMatrix: _scaler.Matrix);

            if (!_session.InCutscene)
            {
                _hud.Draw(_spriteBatch, _renderer, _fonts.Name, _fonts.Small, _session);
                if (!_session.Playing)
                    _resultOverlay.Draw(_spriteBatch, _renderer, _fonts, _session);
            }

            _spriteBatch.End();
        }

        base.Draw(gameTime);

        if (ScreenshotPath != null && ++_framesDrawn >= 90)
        {
            CaptureScreenshot(ScreenshotPath);
            Exit();
        }
    }

    private void CaptureScreenshot(string path)
    {
        int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int height = GraphicsDevice.PresentationParameters.BackBufferHeight;
        var pixels = new Color[width * height];
        GraphicsDevice.GetBackBufferData(pixels);

        using var texture = new Texture2D(GraphicsDevice, width, height);
        texture.SetData(pixels);

        string? dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var stream = File.Create(path);
        texture.SaveAsPng(stream, width, height);
        Console.WriteLine($"screenshot saved: {path} ({width}x{height})");
    }
}
