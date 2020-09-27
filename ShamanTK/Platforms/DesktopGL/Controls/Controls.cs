/* 
 * ShamanTK
 * A toolkit for creating multimedia applications.
 * Copyright (C) 2020, Maximilian Bauer (contact@lengo.cc)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using ShamanTK.Controls;
using System.Numerics;
using OpenTK.Input;
using System.Text;
using System.Drawing;

namespace ShamanTK.Platforms.DesktopGL.Controls
{
    /// <summary>
    /// Provides access to the human interface devices of the current platform.
    /// </summary>
    class Controls : IControls
    {
        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a keyboard (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        public bool SupportsKeyboard => keyboard.IsConnected;

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a mouse (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        public bool SupportsMouse => mouse.IsConnected;

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a accelerometer (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        public virtual bool SupportsAccelerometer { get; } = false;

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a gyroscope (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        public virtual bool SupportsGyroscope { get; } = false;

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a touch screen (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        public virtual bool SupportsTouch { get; } = false;

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a gamepad (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        public bool SupportsGamepads { get; } = true;

        /// <summary>
        /// Gets the amount of currently connected gamepads or 0, if no 
        /// gamepads are available or <see cref="SupportsGamepads"/> is
        /// <c>false</c>.
        /// </summary>
        public int AvailableGamepadsCount { get; }

        private readonly Graphics.Graphics graphics;
        private OpenTK.GameWindow Window => graphics.Window;
        private KeyboardState keyboard;
        private MouseState mouse, mousePrevious;
        private MouseMode mouseMode, mouseModePrevious;
        private Vector3 mouseSpeed;
        private readonly GamePadState[] gamepads;
        private readonly StringBuilder typedCharacters = new StringBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="Controls"/> class.
        /// </summary>
        /// <param name="graphics">
        /// The associated <see cref="Graphics.Graphics"/> unit, which is used
        /// for synchronizing the new <see cref="Controls"/> instance with
        /// the update cycle.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="window"/> or
        /// <paramref name="settings"/> are null.
        /// </exception>
        public Controls(Graphics.Graphics graphics)
        {
            this.graphics = graphics ??
                throw new ArgumentNullException(nameof(graphics));

            graphics.Window.KeyPress += KeyTyped;
            graphics.PreUpdate += FramePreUpdate;
            graphics.PostUpdate += FramePostUpdate;

            int connectedGamepads = 0;
            for (int i = 0; i < 4; i++)
            {
                if (GamePad.GetCapabilities(i).IsConnected)
                    connectedGamepads = i + 1;
                else break;
            }
            gamepads = new GamePadState[connectedGamepads];

            //Perform an initial state update to ensure the properties of
            //the implementeted IControls interface return valid values.
            FramePreUpdate(this, new OpenTK.FrameEventArgs(0.1));
        }

        private void FramePostUpdate(object sender, EventArgs e)
        {
            typedCharacters.Clear();
        }

        private const float MouseWheelMax = 5.0f;
        private const float MouseMovementDampFactor = 0.02f;

        private void FramePreUpdate(object sender, EventArgs e)
        {
            keyboard = Keyboard.GetState();

            for (int i = 0; i < gamepads.Length; i++)
                gamepads[i] = GamePad.GetState(i);

            bool mouseModeChanged = mouseModePrevious != mouseMode;

            mousePrevious = mouse;
            mouse = Mouse.GetCursorState();
            mouseModePrevious = mouseMode;

            Vector2 mouseOrigin;

            if (mouseMode == MouseMode.InvisibleFixed
                && graphics.Window.Focused)
            {
                Window.CursorVisible = false;
                Window.Cursor = OpenTK.MouseCursor.Empty;
                Mouse.SetPosition(Window.X + (Window.Width / 2.0),
                    Window.Y + (Window.Height / 2.0));
                //Required to reliably compare the position of the mouse 
                //before and after moving it to the center of the screen.
                MouseState centeredState = Mouse.GetCursorState();
                mouseOrigin = new Vector2(centeredState.X,
                    centeredState.Y);
            }
            else
            {
                mouseOrigin = new Vector2(mousePrevious.X,
                    mousePrevious.Y);
                Window.CursorVisible = true;
                Window.Cursor = OpenTK.MouseCursor.Default;
            }

            //Prevent that the moving of the mouse to the center position
            //gets misinterpreted as rapid movement and returned as speed.
            if (mouseModeChanged || 
            //Fixes an issue which returns a high speed for touch screens
            //when they are touched the first time (and this touch is used
            //as trigger to get the current speed).
                (!mousePrevious.IsAnyButtonDown && mouse.IsAnyButtonDown))
            {
                mouseSpeed = Vector3.Zero;
                mousePrevious = mouse;
            }
            else
            {
                mouseSpeed = new Vector3(
                    (mouse.X - mouseOrigin.X) * MouseMovementDampFactor,
                    (mouseOrigin.Y - mouse.Y) * MouseMovementDampFactor,
                    Math.Min(1.0f, Math.Max(-1.0f, (mouse.WheelPrecise - 
                        mousePrevious.WheelPrecise) / MouseWheelMax)));
            }
        }

        private void KeyTyped(object sender, 
            OpenTK.KeyPressEventArgs e)
        {
            if (!Window.Focused) return;
            typedCharacters.Append(e.KeyChar);
        }

        /// <summary>
        /// Checks if a button is pressed or not.
        /// </summary>
        /// <param name="button">
        /// The button which should be checked.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified key is pressed, <c>false</c>
        /// if the button is not pressed or <see cref="SupportsKeyboard"/> 
        /// is <c>false</c>.
        /// </returns>
        public bool IsPressed(KeyboardKey button)
        {
            if (!Window.Focused) return false;
            return button switch
            {
                KeyboardKey.A => keyboard.IsKeyDown(Key.A),
                KeyboardKey.Alt => keyboard.IsKeyDown(Key.AltLeft)
                || keyboard.IsKeyDown(Key.AltRight),
                KeyboardKey.Apostrophe => keyboard.IsKeyDown(Key.Quote),
                KeyboardKey.B => keyboard.IsKeyDown(Key.B),
                KeyboardKey.Backslash => keyboard.IsKeyDown(Key.BackSlash),
                KeyboardKey.Backspace => keyboard.IsKeyDown(Key.BackSpace),
                KeyboardKey.BracketLeft => keyboard.IsKeyDown(Key.BracketLeft),
                KeyboardKey.BracketRight => keyboard.IsKeyDown(Key.BracketRight),
                KeyboardKey.C => keyboard.IsKeyDown(Key.C),
                KeyboardKey.CapsLock => keyboard.IsKeyDown(Key.CapsLock),
                KeyboardKey.Circumflex => keyboard.IsKeyDown(Key.Grave),
                KeyboardKey.Comma => keyboard.IsKeyDown(Key.Comma),
                KeyboardKey.Control => keyboard.IsKeyDown(Key.ControlLeft) 
                || keyboard.IsKeyDown(Key.ControlRight),
                KeyboardKey.D => keyboard.IsKeyDown(Key.D),
                KeyboardKey.Delete => keyboard.IsKeyDown(Key.Delete),
                KeyboardKey.Down => keyboard.IsKeyDown(Key.Down),
                KeyboardKey.E => keyboard.IsKeyDown(Key.E),
                KeyboardKey.End => keyboard.IsKeyDown(Key.End),
                KeyboardKey.Enter => keyboard.IsKeyDown(Key.Enter),
                KeyboardKey.Equal => keyboard.IsKeyDown(Key.Plus),//?
                KeyboardKey.Escape => keyboard.IsKeyDown(Key.Escape),
                KeyboardKey.F => keyboard.IsKeyDown(Key.F),
                KeyboardKey.F1 => keyboard.IsKeyDown(Key.F1),
                KeyboardKey.F2 => keyboard.IsKeyDown(Key.F2),
                KeyboardKey.F3 => keyboard.IsKeyDown(Key.F3),
                KeyboardKey.F4 => keyboard.IsKeyDown(Key.F4),
                KeyboardKey.F5 => keyboard.IsKeyDown(Key.F5),
                KeyboardKey.F6 => keyboard.IsKeyDown(Key.F6),
                KeyboardKey.F7 => keyboard.IsKeyDown(Key.F7),
                KeyboardKey.F8 => keyboard.IsKeyDown(Key.F8),
                KeyboardKey.F9 => keyboard.IsKeyDown(Key.F9),
                KeyboardKey.F10 => keyboard.IsKeyDown(Key.F10),
                KeyboardKey.F11 => keyboard.IsKeyDown(Key.F11),
                KeyboardKey.F12 => keyboard.IsKeyDown(Key.F12),
                KeyboardKey.Home => keyboard.IsKeyDown(Key.Home),
                KeyboardKey.Hyphen => keyboard.IsKeyDown(Key.Minus),//?
                KeyboardKey.I => keyboard.IsKeyDown(Key.I),
                KeyboardKey.Insert => keyboard.IsKeyDown(Key.Insert),
                KeyboardKey.J => keyboard.IsKeyDown(Key.J),
                KeyboardKey.K => keyboard.IsKeyDown(Key.K),
                KeyboardKey.Keypad0 => keyboard.IsKeyDown(Key.Keypad0),
                KeyboardKey.Keypad1 => keyboard.IsKeyDown(Key.Keypad1),
                KeyboardKey.Keypad2 => keyboard.IsKeyDown(Key.Keypad2),
                KeyboardKey.Keypad3 => keyboard.IsKeyDown(Key.Keypad3),
                KeyboardKey.Keypad4 => keyboard.IsKeyDown(Key.Keypad4),
                KeyboardKey.Keypad5 => keyboard.IsKeyDown(Key.Keypad5),
                KeyboardKey.Keypad6 => keyboard.IsKeyDown(Key.Keypad6),
                KeyboardKey.Keypad7 => keyboard.IsKeyDown(Key.Keypad7),
                KeyboardKey.Keypad8 => keyboard.IsKeyDown(Key.Keypad8),
                KeyboardKey.Keypad9 => keyboard.IsKeyDown(Key.Keypad9),
                KeyboardKey.KeypadDivide => 
                keyboard.IsKeyDown(Key.KeypadDivide),
                KeyboardKey.KeypadEnter => keyboard.IsKeyDown(Key.KeypadEnter),
                KeyboardKey.KeypadMinus => keyboard.IsKeyDown(Key.KeypadMinus),
                KeyboardKey.KeypadMultiply => 
                keyboard.IsKeyDown(Key.KeypadMultiply),
                KeyboardKey.KeypadPlus => keyboard.IsKeyDown(Key.KeypadPlus),
                KeyboardKey.L => keyboard.IsKeyDown(Key.L),
                KeyboardKey.Left => keyboard.IsKeyDown(Key.Left),
                KeyboardKey.M => keyboard.IsKeyDown(Key.M),
                KeyboardKey.N => keyboard.IsKeyDown(Key.N),
                KeyboardKey.Number0 => keyboard.IsKeyDown(Key.Number0),
                KeyboardKey.Number1 => keyboard.IsKeyDown(Key.Number1),
                KeyboardKey.Number2 => keyboard.IsKeyDown(Key.Number2),
                KeyboardKey.Number3 => keyboard.IsKeyDown(Key.Number3),
                KeyboardKey.Number4 => keyboard.IsKeyDown(Key.Number4),
                KeyboardKey.Number5 => keyboard.IsKeyDown(Key.Number5),
                KeyboardKey.Number6 => keyboard.IsKeyDown(Key.Number6),
                KeyboardKey.Number7 => keyboard.IsKeyDown(Key.Number7),
                KeyboardKey.Number8 => keyboard.IsKeyDown(Key.Number8),
                KeyboardKey.Number9 => keyboard.IsKeyDown(Key.Number9),
                KeyboardKey.O => keyboard.IsKeyDown(Key.O),
                KeyboardKey.P => keyboard.IsKeyDown(Key.P),
                KeyboardKey.PageDown => keyboard.IsKeyDown(Key.PageDown),
                KeyboardKey.PageUp => keyboard.IsKeyDown(Key.PageUp),
                KeyboardKey.PauseBreak => keyboard.IsKeyDown(Key.Pause),
                KeyboardKey.Period => keyboard.IsKeyDown(Key.Period),
                KeyboardKey.Print => keyboard.IsKeyDown(Key.PrintScreen),
                KeyboardKey.Q => keyboard.IsKeyDown(Key.Q),
                KeyboardKey.R => keyboard.IsKeyDown(Key.R),
                KeyboardKey.Right => keyboard.IsKeyDown(Key.Right),
                KeyboardKey.S => keyboard.IsKeyDown(Key.S),
                KeyboardKey.ScrollLock => keyboard.IsKeyDown(Key.ScrollLock),
                KeyboardKey.Semicolon => keyboard.IsKeyDown(Key.Semicolon),
                KeyboardKey.Shift => keyboard.IsKeyDown(Key.ShiftLeft) 
                || keyboard.IsKeyDown(Key.ShiftRight),
                KeyboardKey.Slash => keyboard.IsKeyDown(Key.Slash),
                KeyboardKey.Space => keyboard.IsKeyDown(Key.Space),
                KeyboardKey.T => keyboard.IsKeyDown(Key.T),
                KeyboardKey.Tab => keyboard.IsKeyDown(Key.Tab),
                KeyboardKey.Tilde => keyboard.IsKeyDown(Key.Tilde),
                KeyboardKey.U => keyboard.IsKeyDown(Key.U),
                KeyboardKey.Up => keyboard.IsKeyDown(Key.Up),
                KeyboardKey.V => keyboard.IsKeyDown(Key.V),
                KeyboardKey.W => keyboard.IsKeyDown(Key.W),
                KeyboardKey.WindowsCommand => keyboard.IsKeyDown(Key.WinLeft) 
                || keyboard.IsKeyDown(Key.WinRight),
                KeyboardKey.X => keyboard.IsKeyDown(Key.X),
                KeyboardKey.Y => keyboard.IsKeyDown(Key.Y),
                KeyboardKey.Z => keyboard.IsKeyDown(Key.Z),
                KeyboardKey.None => false,
                _ => false,
            };
        }

        /// <summary>
        /// Checks if a button is pressed or not.
        /// </summary>
        /// <param name="button">
        /// The button which should be checked.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified button is pressed, <c>false</c>
        /// if the key is not pressed or <see cref="SupportsMouse"/> is 
        /// <c>false</c>.
        /// </returns>
        public bool IsPressed(ShamanTK.Controls.MouseButton button)
        {
            if (!Window.Focused)
                return false;
            return button switch
            {
                ShamanTK.Controls.MouseButton.Left => 
                mouse.IsButtonDown(OpenTK.Input.MouseButton.Left),
                ShamanTK.Controls.MouseButton.Right => 
                mouse.IsButtonDown(OpenTK.Input.MouseButton.Right),
                ShamanTK.Controls.MouseButton.Middle => 
                mouse.IsButtonDown(OpenTK.Input.MouseButton.Middle),
                ShamanTK.Controls.MouseButton.Extra1 => 
                mouse.IsButtonDown(OpenTK.Input.MouseButton.Button1),
                ShamanTK.Controls.MouseButton.Extra2 => 
                mouse.IsButtonDown(OpenTK.Input.MouseButton.Button2),
                ShamanTK.Controls.MouseButton.Extra3 => 
                mouse.IsButtonDown(OpenTK.Input.MouseButton.Button3),
                ShamanTK.Controls.MouseButton.None => false,
                _ => false,
            };
        }

        /// <summary>
        /// Checks if a button is pressed or not.
        /// </summary>
        /// <param name="button">
        /// The button which should be checked.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the connected gamepad.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified button is pressed, <c>false</c>
        /// if <see cref="SupportsGamepads"/> is <c>false</c> or 
        /// <paramref name="gamepadIndex"/> is greater than/equal to
        /// <see cref="AvailableGamepadsCount"/>.
        /// </returns>
        public virtual bool IsPressed(GamepadButton button, int gamepadIndex)
        {
            if (!Window.Focused) return false;
            if (gamepadIndex < gamepads.Length)
            {
                GamePadState gamepad = gamepads[gamepadIndex];
                return button switch
                {
                    GamepadButton.A => 
                    gamepad.Buttons.A == ButtonState.Pressed,
                    GamepadButton.B => 
                    gamepad.Buttons.B == ButtonState.Pressed,
                    GamepadButton.X => 
                    gamepad.Buttons.X == ButtonState.Pressed,
                    GamepadButton.Y => 
                    gamepad.Buttons.Y == ButtonState.Pressed,
                    GamepadButton.BigButton => 
                    gamepad.Buttons.BigButton == ButtonState.Pressed,
                    GamepadButton.Start => 
                    gamepad.Buttons.Start == ButtonState.Pressed,
                    GamepadButton.Back => 
                    gamepad.Buttons.Back == ButtonState.Pressed,
                    GamepadButton.LeftShoulder => 
                    gamepad.Buttons.LeftShoulder == ButtonState.Pressed,
                    GamepadButton.RightShoulder => 
                    gamepad.Buttons.RightShoulder == ButtonState.Pressed,
                    GamepadButton.LeftStick => 
                    gamepad.Buttons.LeftStick == ButtonState.Pressed,
                    GamepadButton.RightStick => 
                    gamepad.Buttons.RightStick == ButtonState.Pressed,
                    GamepadButton.DPadDown => 
                    gamepad.DPad.Down == ButtonState.Pressed,
                    GamepadButton.DPadLeft => 
                    gamepad.DPad.Left == ButtonState.Pressed,
                    GamepadButton.DPadRight => 
                    gamepad.DPad.Right == ButtonState.Pressed,
                    GamepadButton.DPadUp => 
                    gamepad.DPad.Up == ButtonState.Pressed,
                    GamepadButton.None => 
                    false,
                    _ => false,
                };
            }
            return false;
        }

        /// <summary>
        /// Checks if the touch screen is currently touched.
        /// </summary>
        /// <param name="touchPoint">
        /// The index of the touch point ("finger"). 
        /// </param>
        /// <param name="position">
        /// The position of the touch point in relative canvas coordinates
        /// with its origin in the upper left corner of the window/canvas 
        /// and component values between 0.0 and 1.0, or 
        /// <see cref="Vector2.Zero"/>, if the touch point isn't active 
        /// or <see cref="SupportsTouch"/> is <c>false</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the touch point is currently touched,
        /// <c>false</c> otherwise, or when <see cref="SupportsTouch"/>
        /// is <c>false</c>.
        /// </returns>
        public virtual bool IsTouched(int touchPoint, out Vector2 position)
        {
            position = Vector2.Zero;
            return false;
        }

        /// <summary>
        /// Gets the current value of the specified axis.
        /// </summary>
        /// <param name="axis">
        /// The axis which should be queried.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad which should be queried.
        /// </param>
        /// <returns>
        /// The value of the specified axis as a decimal value between 0.0
        /// and 1.0, or 0.0 when the axis is in its default "idle" state,
        /// the opposing axis is having a value greater than 0,
        /// when <see cref="SupportsGamepads"/> is <c>false</c> or when
        /// <paramref name="gamepadIndex"/> is greater than/equal to
        /// <see cref="AvailableGamepadsCount"/>.
        /// </returns>
        public float GetGamepadAxis(GamepadAxis axis, int gamepadIndex)
        {
            if (!Window.Focused) return 0;
            GamePadState state = GamePad.GetState(gamepadIndex);
            if (!state.IsConnected) return 0;
            var value = axis switch
            {
                GamepadAxis.LeftStickRight => 
                Math.Max(state.ThumbSticks.Left.X, 0),
                GamepadAxis.LeftStickLeft => 
                Math.Abs(Math.Min(state.ThumbSticks.Left.X, 0)),
                GamepadAxis.LeftStickUp => 
                Math.Max(state.ThumbSticks.Left.Y, 0),
                GamepadAxis.LeftStickDown => 
                Math.Abs(Math.Min(state.ThumbSticks.Left.Y, 0)),
                GamepadAxis.RightStickRight => 
                Math.Max(state.ThumbSticks.Right.X, 0),
                GamepadAxis.RightStickLeft => 
                Math.Abs(Math.Min(state.ThumbSticks.Right.X, 0)),
                GamepadAxis.RightStickUp => 
                Math.Max(state.ThumbSticks.Right.Y, 0),
                GamepadAxis.RightStickDown => 
                Math.Abs(Math.Min(state.ThumbSticks.Right.Y, 0)),
                GamepadAxis.LeftTrigger => state.Triggers.Left,
                GamepadAxis.RightTrigger => state.Triggers.Right,
                GamepadAxis.None => 0,
                _ => 0,
            };
            return Math.Min(1.0f, Math.Max(value - 0.1f, 0) / 0.9f);
        }

        /// <summary>
        /// Gets the current accerlation of the mouse in a certain direction.
        /// </summary>
        /// <param name="axis">
        /// The axis which should be queried.
        /// </param>
        /// <returns>
        /// The returned <see cref="float"/> will be 0.0 if the mouse isn't
        /// moved>. A value greater than 0.0 and smaller than 1.0 specifies a 
        /// normal movement in the ranges that could also be reached by pushing
        /// a key, button or a gamepad axis fully into one direction. Values 
        /// greater than 1.0 are possible and indicate a faster movement than
        /// what could be reached by pressing a button or a gamepad stick.
        /// </returns>
        public float GetMouseSpeed(MouseSpeedAxis axis)
        {
            if (!Window.Focused) return 0;
            var value = axis switch
            {
                MouseSpeedAxis.Up => Math.Max(0, mouseSpeed.Y),
                MouseSpeedAxis.Right => Math.Max(0, mouseSpeed.X),
                MouseSpeedAxis.Down => Math.Max(0, -mouseSpeed.Y),
                MouseSpeedAxis.Left => Math.Max(0, -mouseSpeed.X),
                MouseSpeedAxis.WheelUp => Math.Max(0, mouseSpeed.Z),
                MouseSpeedAxis.WheelDown => Math.Max(0, -mouseSpeed.Z),
                _ => 0,
            };
            return value;
        }

        /// <summary>
        /// Gets the current mouse position in relative canvas coordinates.
        /// </summary>
        /// <returns>
        /// The mouse position as <see cref="Vector2"/>, which specifies the
        /// current mouse position in relative canvas coordinates
        /// with its origin in the upper left corner of the window/canvas
        /// and component values between 0.0 and 1.0.
        /// </returns>
        public Vector2 GetMousePosition()
        {
            if (!Window.Focused) return Vector2.Zero;
            Point position = Window.PointToClient(new Point(mouse.X, mouse.Y));
            Vector2 relativePosition = new Vector2(
                position.X / (float)Window.Width,
                position.Y / (float)Window.Height);
            return relativePosition;            
        }

        /// <summary>
        /// Gets the current value of the accelerometer as normalized
        /// <see cref="Vector3"/>.
        /// </summary>
        /// <returns>
        /// A new instance of the <see cref="Vector3"/> struct or
        /// <see cref="Vector3.Zero"/>, when 
        /// <see cref="SupportsAccelerometer"/> is <c>false</c>.
        /// </returns>
        public virtual Vector3 GetAccelerometer()
        {
            return Vector3.Zero;
        }

        /// <summary>
        /// Gets the current value of the gyroscope as normalized
        /// <see cref="Vector3"/>.
        /// </summary>
        /// <returns>
        /// A new instance of the <see cref="Vector3"/> struct or
        /// <see cref="Vector3.Zero"/>, when 
        /// <see cref="SupportsGyroscope"/> is <c>false</c>.
        /// </returns>
        public virtual Vector3 GetGyroscope()
        {
            return Vector3.Zero;
        }

        /// <summary>
        /// Gets the characters typed on the keyboard since the last update,
        /// using the currently active keyboard layout. This does not include
        /// any control characters beside whitespaces.
        /// </summary>
        /// <returns>
        /// A string of typed characters or an empty string, if no characters
        /// were typed or <see cref="SupportsKeyboard"/> is <c>false</c>.
        /// </returns>
        public string GetTypedCharacters()
        {
            return typedCharacters.ToString();
        }

        /// <summary>
        /// Changes the mode of the mouse.
        /// </summary>
        /// <param name="mode">
        /// The new mode of the mouse.
        /// </param>
        /// <remarks>
        /// If the application is minimized or looses focus, the mouse must 
        /// be available as usual. If the application regains focus, the 
        /// specified <see cref="MouseMode"/> must be re-applied again. If 
        /// <see cref="SupportsMouse"/> is <c>false</c>, calling this method 
        /// should have no effect.
        /// </remarks>
        public void SetMouse(MouseMode mode)
        {
            mouseMode = mode;
        }
    }
}
