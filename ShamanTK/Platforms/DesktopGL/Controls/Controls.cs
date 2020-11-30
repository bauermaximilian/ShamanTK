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
using System.Text;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using GlfwMouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using ShamanMouseButton = ShamanTK.Controls.MouseButton;

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
        public bool SupportsKeyboard => true;

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a mouse (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        public bool SupportsMouse => true;

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a accelerometer (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        public virtual bool SupportsAccelerometer => false;

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a gyroscope (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        public virtual bool SupportsGyroscope => false;

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a touch screen (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        public virtual bool SupportsTouch => false;

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a gamepad (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        public bool SupportsGamepads => true;

        /// <summary>
        /// Gets the amount of currently connected gamepads or 0, if no 
        /// gamepads are available or <see cref="SupportsGamepads"/> is
        /// <c>false</c>.
        /// </summary>
        public int AvailableGamepadsCount { get; private set; }

        private readonly Graphics.Graphics graphics;

        private GameWindow Window => graphics.Window;
        private readonly StringBuilder typedCharacters = new StringBuilder();

        private MouseMode mouseMode;
        private Vector2 mouseSpeed;
        private float mouseWheelSpeed;
        private bool mouseModeChanged = true;

        private readonly GamepadState[] gamepadStates;

        private const float MouseWheelMax = 5.0f;
        private const float MouseDamp = 0.02f;

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

            graphics.Window.TextInput += KeyTyped;
            graphics.Window.MouseWheel += MouseWheelMoved;
            graphics.PreUpdate += FramePreUpdate;
            graphics.PostUpdate += FramePostUpdate;

            gamepadStates = new GamepadState[Window.JoystickStates.Count];

            // Perform an initial state update to ensure the properties of
            // the implementeted IControls interface return valid values.
            FramePreUpdate(this, EventArgs.Empty);
        }

        private void MouseWheelMoved(MouseWheelEventArgs obj)
        {
            if (!Window.IsFocused) return;
            mouseWheelSpeed += obj.OffsetY;
        }

        private void KeyTyped(TextInputEventArgs e)
        {
            if (!Window.IsFocused) return;
            typedCharacters.Append(e.AsString);
        }

        private void FramePostUpdate(object sender, EventArgs e)
        {
            typedCharacters.Clear();
            mouseWheelSpeed = 0;
        }

        private void FramePreUpdate(object sender, EventArgs e)
        {
            Vector2 mouseOrigin;
            Vector2 windowCenter = new Vector2(
                Window.Size.X / 2.0f, Window.Size.Y / 2.0f);

            if (mouseMode == MouseMode.InvisibleFixed
                && graphics.Window.IsFocused)
            {
                Window.CursorVisible = false;
                Window.Cursor = MouseCursor.Empty;

                // Required to reliably compare the position of the mouse 
                // before and after moving it to the center of the screen.
                mouseOrigin = new Vector2(Window.MouseState.PreviousX,
                     Window.MouseState.PreviousY);

                Window.MousePosition = new OpenTK.Mathematics.Vector2(
                    windowCenter.X, windowCenter.Y);
            }
            else
            {
                mouseOrigin = new Vector2(Window.MouseState.PreviousX,
                    Window.MouseState.PreviousY);
                Window.CursorVisible = true;
                Window.Cursor = MouseCursor.Default;
            }

            // Prevent that the moving of the mouse to the center position
            // gets misinterpreted as rapid movement and returned as speed.
            if (mouseModeChanged ||
                // Fixes an issue which returns a high speed for touch screens
                // when they are touched the first time (and this touch is used
                // as trigger to get the current speed).
                (!Window.MouseState.WasButtonDown(GlfwMouseButton.Left) &&
                Window.MouseState.IsButtonDown(GlfwMouseButton.Left)))
            {
                mouseSpeed = Vector2.Zero;
                mouseModeChanged = false;
            }
            else
            {
                mouseSpeed = new Vector2(
                    (mouseOrigin.X - windowCenter.X) * MouseDamp,
                    (windowCenter.Y - mouseOrigin.Y) * MouseDamp);
            }

            AvailableGamepadsCount = 0;
            for (int i = 0; i < gamepadStates.Length; i++)
            {
                if (Window.JoystickStates[i] != null &&
                    GLFW.GetGamepadState(Window.JoystickStates[i].Id,
                    out GamepadState gamepadState))
                {
                    gamepadStates[i] = gamepadState;
                    AvailableGamepadsCount = Math.Max(AvailableGamepadsCount, 
                        i + 1);
                }
                else gamepadStates[i] = new GamepadState();
            }
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
            if (!Window.IsFocused) return false;
            KeyboardState keyboard = Window.KeyboardState;
            return button switch
            {
                KeyboardKey.A => keyboard.IsKeyDown(Keys.A),
                KeyboardKey.Alt => keyboard.IsKeyDown(Keys.LeftAlt)
                    || keyboard.IsKeyDown(Keys.RightAlt),
                KeyboardKey.Apostrophe => keyboard.IsKeyDown(Keys.Apostrophe),
                KeyboardKey.B => keyboard.IsKeyDown(Keys.B),
                KeyboardKey.Backslash => keyboard.IsKeyDown(Keys.Backslash),
                KeyboardKey.Backspace => keyboard.IsKeyDown(Keys.Backspace),
                KeyboardKey.BracketLeft => keyboard.IsKeyDown(Keys.LeftBracket),
                KeyboardKey.BracketRight => keyboard.IsKeyDown(Keys.RightBracket),
                KeyboardKey.C => keyboard.IsKeyDown(Keys.C),
                KeyboardKey.CapsLock => keyboard.IsKeyDown(Keys.CapsLock),
                KeyboardKey.Comma => keyboard.IsKeyDown(Keys.Comma),
                KeyboardKey.Control => keyboard.IsKeyDown(Keys.LeftControl)
                    || keyboard.IsKeyDown(Keys.RightControl),
                KeyboardKey.D => keyboard.IsKeyDown(Keys.D),
                KeyboardKey.Delete => keyboard.IsKeyDown(Keys.Delete),
                KeyboardKey.Down => keyboard.IsKeyDown(Keys.Down),
                KeyboardKey.E => keyboard.IsKeyDown(Keys.E),
                KeyboardKey.End => keyboard.IsKeyDown(Keys.End),
                KeyboardKey.Enter => keyboard.IsKeyDown(Keys.Enter),
                KeyboardKey.Equal => keyboard.IsKeyDown(Keys.Equal),
                KeyboardKey.Escape => keyboard.IsKeyDown(Keys.Escape),
                KeyboardKey.F => keyboard.IsKeyDown(Keys.F),
                KeyboardKey.F1 => keyboard.IsKeyDown(Keys.F1),
                KeyboardKey.F2 => keyboard.IsKeyDown(Keys.F2),
                KeyboardKey.F3 => keyboard.IsKeyDown(Keys.F3),
                KeyboardKey.F4 => keyboard.IsKeyDown(Keys.F4),
                KeyboardKey.F5 => keyboard.IsKeyDown(Keys.F5),
                KeyboardKey.F6 => keyboard.IsKeyDown(Keys.F6),
                KeyboardKey.F7 => keyboard.IsKeyDown(Keys.F7),
                KeyboardKey.F8 => keyboard.IsKeyDown(Keys.F8),
                KeyboardKey.F9 => keyboard.IsKeyDown(Keys.F9),
                KeyboardKey.F10 => keyboard.IsKeyDown(Keys.F10),
                KeyboardKey.F11 => keyboard.IsKeyDown(Keys.F11),
                KeyboardKey.F12 => keyboard.IsKeyDown(Keys.F12),
                KeyboardKey.Home => keyboard.IsKeyDown(Keys.Home),
                KeyboardKey.Hyphen => keyboard.IsKeyDown(Keys.Minus),
                KeyboardKey.I => keyboard.IsKeyDown(Keys.I),
                KeyboardKey.Insert => keyboard.IsKeyDown(Keys.Insert),
                KeyboardKey.J => keyboard.IsKeyDown(Keys.J),
                KeyboardKey.K => keyboard.IsKeyDown(Keys.K),
                KeyboardKey.Keypad0 => keyboard.IsKeyDown(Keys.KeyPad0),
                KeyboardKey.Keypad1 => keyboard.IsKeyDown(Keys.KeyPad1),
                KeyboardKey.Keypad2 => keyboard.IsKeyDown(Keys.KeyPad2),
                KeyboardKey.Keypad3 => keyboard.IsKeyDown(Keys.KeyPad3),
                KeyboardKey.Keypad4 => keyboard.IsKeyDown(Keys.KeyPad4),
                KeyboardKey.Keypad5 => keyboard.IsKeyDown(Keys.KeyPad5),
                KeyboardKey.Keypad6 => keyboard.IsKeyDown(Keys.KeyPad6),
                KeyboardKey.Keypad7 => keyboard.IsKeyDown(Keys.KeyPad7),
                KeyboardKey.Keypad8 => keyboard.IsKeyDown(Keys.KeyPad8),
                KeyboardKey.Keypad9 => keyboard.IsKeyDown(Keys.KeyPad9),
                KeyboardKey.KeypadDivide =>
                    keyboard.IsKeyDown(Keys.KeyPadDivide),
                KeyboardKey.KeypadEnter =>
                    keyboard.IsKeyDown(Keys.KeyPadEnter),
                KeyboardKey.KeypadMinus =>
                    keyboard.IsKeyDown(Keys.KeyPadSubtract),
                KeyboardKey.KeypadMultiply =>
                    keyboard.IsKeyDown(Keys.KeyPadMultiply),
                KeyboardKey.KeypadPlus => keyboard.IsKeyDown(Keys.KeyPadAdd),
                KeyboardKey.L => keyboard.IsKeyDown(Keys.L),
                KeyboardKey.Left => keyboard.IsKeyDown(Keys.Left),
                KeyboardKey.M => keyboard.IsKeyDown(Keys.M),
                KeyboardKey.N => keyboard.IsKeyDown(Keys.N),
                KeyboardKey.N0 => keyboard.IsKeyDown(Keys.D0),
                KeyboardKey.N1 => keyboard.IsKeyDown(Keys.D1),
                KeyboardKey.N2 => keyboard.IsKeyDown(Keys.D2),
                KeyboardKey.N3 => keyboard.IsKeyDown(Keys.D3),
                KeyboardKey.N4 => keyboard.IsKeyDown(Keys.D4),
                KeyboardKey.N5 => keyboard.IsKeyDown(Keys.D5),
                KeyboardKey.N6 => keyboard.IsKeyDown(Keys.D6),
                KeyboardKey.N7 => keyboard.IsKeyDown(Keys.D7),
                KeyboardKey.N8 => keyboard.IsKeyDown(Keys.D8),
                KeyboardKey.N9 => keyboard.IsKeyDown(Keys.D9),
                KeyboardKey.O => keyboard.IsKeyDown(Keys.O),
                KeyboardKey.P => keyboard.IsKeyDown(Keys.P),
                KeyboardKey.PageDown => keyboard.IsKeyDown(Keys.PageDown),
                KeyboardKey.PageUp => keyboard.IsKeyDown(Keys.PageUp),
                KeyboardKey.PauseBreak => keyboard.IsKeyDown(Keys.Pause),
                KeyboardKey.Period => keyboard.IsKeyDown(Keys.Period),
                KeyboardKey.Print => keyboard.IsKeyDown(Keys.PrintScreen),
                KeyboardKey.Q => keyboard.IsKeyDown(Keys.Q),
                KeyboardKey.R => keyboard.IsKeyDown(Keys.R),
                KeyboardKey.Right => keyboard.IsKeyDown(Keys.Right),
                KeyboardKey.S => keyboard.IsKeyDown(Keys.S),
                KeyboardKey.ScrollLock => keyboard.IsKeyDown(Keys.ScrollLock),
                KeyboardKey.Semicolon => keyboard.IsKeyDown(Keys.Semicolon),
                KeyboardKey.Shift => keyboard.IsKeyDown(Keys.LeftShift)
                    || keyboard.IsKeyDown(Keys.RightShift),
                KeyboardKey.Slash => keyboard.IsKeyDown(Keys.Slash),
                KeyboardKey.Space => keyboard.IsKeyDown(Keys.Space),
                KeyboardKey.T => keyboard.IsKeyDown(Keys.T),
                KeyboardKey.Tab => keyboard.IsKeyDown(Keys.Tab),
                KeyboardKey.GraveAccent =>
                    keyboard.IsKeyDown(Keys.GraveAccent),
                KeyboardKey.U => keyboard.IsKeyDown(Keys.U),
                KeyboardKey.Up => keyboard.IsKeyDown(Keys.Up),
                KeyboardKey.V => keyboard.IsKeyDown(Keys.V),
                KeyboardKey.W => keyboard.IsKeyDown(Keys.W),
                KeyboardKey.Super => keyboard.IsKeyDown(Keys.LeftSuper)
                    || keyboard.IsKeyDown(Keys.RightSuper),
                KeyboardKey.X => keyboard.IsKeyDown(Keys.X),
                KeyboardKey.Y => keyboard.IsKeyDown(Keys.Y),
                KeyboardKey.Z => keyboard.IsKeyDown(Keys.Z),
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
            if (!Window.IsFocused) return false;

            MouseState mouse = Window.MouseState;
            return button switch
            {
                ShamanMouseButton.Left =>
                    mouse.IsButtonDown(GlfwMouseButton.Left),
                ShamanMouseButton.Right =>
                    mouse.IsButtonDown(GlfwMouseButton.Right),
                ShamanMouseButton.Middle =>
                    mouse.IsButtonDown(GlfwMouseButton.Middle),
                ShamanMouseButton.Extra1 =>
                    mouse.IsButtonDown(GlfwMouseButton.Button1),
                ShamanMouseButton.Extra2 =>
                    mouse.IsButtonDown(GlfwMouseButton.Button2),
                ShamanMouseButton.Extra3 =>
                    mouse.IsButtonDown(GlfwMouseButton.Button3),
                ShamanMouseButton.None => false,
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
        public unsafe virtual bool IsPressed(GamepadButton button,
            int gamepadIndex)
        {
            if (!Window.IsFocused) return false;

            if (gamepadIndex < AvailableGamepadsCount)
            {
                GamepadState gamepad = gamepadStates[gamepadIndex];
                const byte down = (byte)JoystickInputAction.Press;

                return button switch
                {
                    GamepadButton.A => gamepad.Buttons[0] == down,
                    GamepadButton.B => gamepad.Buttons[1] == down,
                    GamepadButton.X => gamepad.Buttons[2] == down,
                    GamepadButton.Y => gamepad.Buttons[3] == down,
                    GamepadButton.LeftShoulder => gamepad.Buttons[4] == down,
                    GamepadButton.RightShoulder => gamepad.Buttons[5] == down,
                    GamepadButton.Back => gamepad.Buttons[6] == down,
                    GamepadButton.Start => gamepad.Buttons[7] == down,
                    GamepadButton.BigButton => gamepad.Buttons[8] == down,
                    GamepadButton.LeftStick => gamepad.Buttons[9] == down,
                    GamepadButton.RightStick => gamepad.Buttons[10] == down,
                    GamepadButton.DPadUp => gamepad.Buttons[11] == down,
                    GamepadButton.DPadRight => gamepad.Buttons[12] == down,
                    GamepadButton.DPadDown => gamepad.Buttons[13] == down,
                    GamepadButton.DPadLeft => gamepad.Buttons[14] == down,
                    GamepadButton.None => false,
                    _ => false,
                };
            }
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
        public unsafe float GetGamepadAxis(GamepadAxis axis, int gamepadIndex)
        {
            if (!Window.IsFocused) return 0;

            if (gamepadIndex < AvailableGamepadsCount)
            {
                GamepadState gamepad = gamepadStates[gamepadIndex];

                var value = axis switch
                {
                    GamepadAxis.LeftStickRight =>
                        Math.Max(gamepad.Axes[0], 0),
                    GamepadAxis.LeftStickLeft =>
                        Math.Abs(Math.Min(gamepad.Axes[0], 0)),
                    GamepadAxis.LeftStickUp =>
                        Math.Max(gamepad.Axes[1], 0),
                    GamepadAxis.LeftStickDown =>
                        Math.Abs(Math.Min(gamepad.Axes[1], 0)),
                    GamepadAxis.RightStickRight =>
                        Math.Max(gamepad.Axes[2], 0),
                    GamepadAxis.RightStickLeft =>
                        Math.Abs(Math.Min(gamepad.Axes[2], 0)),
                    GamepadAxis.RightStickUp =>
                        Math.Max(gamepad.Axes[3], 0),
                    GamepadAxis.RightStickDown =>
                        Math.Abs(Math.Min(gamepad.Axes[3], 0)),
                    GamepadAxis.LeftTrigger =>
                        gamepad.Axes[4],
                    GamepadAxis.RightTrigger =>
                        gamepad.Axes[5],
                    GamepadAxis.None => 0,
                    _ => 0,
                };

                return Math.Min(1.0f, Math.Max(value - 0.1f, 0) / 0.9f);
            }
            else return 0;
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
            if (!Window.IsFocused) return 0;

            var value = axis switch
            {
                MouseSpeedAxis.Up => Math.Max(0, mouseSpeed.Y),
                MouseSpeedAxis.Right => Math.Max(0, mouseSpeed.X),
                MouseSpeedAxis.Down => Math.Max(0, -mouseSpeed.Y),
                MouseSpeedAxis.Left => Math.Max(0, -mouseSpeed.X),
                MouseSpeedAxis.WheelUp => Math.Max(0, mouseWheelSpeed),
                MouseSpeedAxis.WheelDown => Math.Min(0, -mouseWheelSpeed),
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
            if (!Window.IsFocused) return Vector2.Zero;

            Vector2 relativePosition = new Vector2(
                Window.MousePosition.X / (float)Window.Size.X,
                Window.MousePosition.Y / (float)Window.Size.Y);
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
            if (mouseMode != mode)
            {
                mouseMode = mode;
                mouseModeChanged = true;
            }
        }
    }
}
