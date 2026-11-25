using DvorFight.Core;
using Microsoft.Xna.Framework.Input;

namespace DvorFight.Input;

/// <summary>
/// Translates keyboard + mouse into held directions and edge-triggered commands.
/// </summary>
public sealed class InputController
{
    public bool IsLeft { get; private set; }
    public bool IsRight { get; private set; }
    public bool IsDown { get; private set; }

    private KeyboardState _prevKeyboard;
    private MouseState _prevMouse;
    private readonly HashSet<Command> _pressed = new();

    public void Update(KeyboardState keyboard, MouseState mouse)
    {
        _pressed.Clear();

        IsLeft = keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A);
        IsRight = keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D);
        IsDown = keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S);

        Edge(keyboard, Keys.Space, Command.Jump);
        Edge(keyboard, Keys.Up, Command.Jump);
        Edge(keyboard, Keys.W, Command.Jump);
        Edge(keyboard, Keys.Z, Command.SpecialZ);
        Edge(keyboard, Keys.X, Command.SpecialX);

        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            _pressed.Add(Command.Hit);

        _prevKeyboard = keyboard;
        _prevMouse = mouse;
    }

    public IReadOnlyList<Command> ConsumeCommands()
    {
        var list = _pressed.ToList();
        _pressed.Clear();
        return list;
    }

    private void Edge(KeyboardState keyboard, Keys key, Command command)
    {
        if (keyboard.IsKeyDown(key) && _prevKeyboard.IsKeyUp(key))
            _pressed.Add(command);
    }
}
