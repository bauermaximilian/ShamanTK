/* 
 * Eterra Framework
 * A simple framework for creating multimedia applications.
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

namespace Eterra.Controls
{
    /// <summary>
    /// Defines the different mouse modes.
    /// </summary>
    public enum MouseMode
    {
        /// <summary>
        /// Defines a freely movable mouse with visible cursor.
        /// </summary>
        VisibleFree,
        /// <summary>
        /// Defines a invisible cursor fixed in the middle for the screen.
        /// This is recommended when only the mouse accerlation is required.
        /// </summary>
        InvisibleFixed
    }

    /// <summary>
    /// Defines the keys on a keyboard in a standard US keyboard layout.
    /// </summary>
    /// <remarks>
    /// See the image in the wikipedia article under the URL 
    /// https://en.wikipedia.org/wiki/Keyboard_layout#Key_types for the 
    /// positions of the keys.
    /// </remarks>
    public enum KeyboardKey
    {
        /// <summary>
        /// Defines the default element, which - when queried - will always 
        /// return false.
        /// </summary>
        None, 
        Escape, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, Print,
        ScrollLock, PauseBreak,
        Circumflex, Number1, Number2, Number3, Number4, Number5, Number6, Number7,
        Number8, Number9, Number0, Hyphen, Equal, Backspace, Insert,
        Home, PageUp,
        Tab, Q, W, E, R, T, Y, U, I, O, P, BracketLeft, BracketRight, Enter,
        Delete, End, PageDown,
        CapsLock, A, S, D, F, J, K, L, Semicolon, Apostrophe, Tilde,
        Shift, Backslash, Z, X, C, V, B, N, M, Comma, Period, Slash, Up,
        Control, WindowsCommand, Alt, Space, Left, Down, Right,
        KeypadDivide, KeypadMultiply, KeypadMinus,
        Keypad7, Keypad8, Keypad9, KeypadPlus,
        Keypad4, Keypad5, Keypad6,
        Keypad1, Keypad2, Keypad3, KeypadEnter,
        Keypad0
    }

    /// <summary>
    /// Defines the buttons on a gamepad.
    /// </summary>
    public enum GamepadButton
    {
        /// <summary>
        /// Defines the default element, which - when queried - will always 
        /// return false.
        /// </summary>
        None,
        /// <summary>
        /// The upper button, which would be a "Y" on a Xbox gamepad.
        /// Is the equivalent of a triangle on PlayStation gamepads. 
        /// </summary>
        Y,
        /// <summary>
        /// The right button, which would be a "B" on a Xbox gamepad.
        /// Is the equivalent of a circle on PlayStation gamepads. 
        /// </summary>
        B,
        /// <summary>
        /// The lower button, which would be a "A" on a Xbox gamepad.
        /// Is the equivalent of a cross on PlayStation gamepads. 
        /// </summary>
        A,
        /// <summary>
        /// The left button, which would be a "X" on a Xbox gamepad.
        /// Is the equivalent of a square on PlayStation gamepads. 
        /// </summary>
        X,
        /// <summary>
        /// The start button, which would be the "Start" button on a Xbox and
        /// a PlayStation 3 gamepad or the "Options" button on a PlayStation 4
        /// gamepad.
        /// </summary>
        Start,
        /// <summary>
        /// The back button, which would be the "Back" button on a Xbox 
        /// gamepad, the "Select" button on a PlayStation 3 gamepad or the 
        /// "Share" button on a PlayStation 4 gamepad.
        /// </summary>
        Back,
        /// <summary>
        /// The "big" button, which would be the guide button on a Xbox 
        /// gamepad or the big touchpad button on a PlayStation 4 gamepad.
        /// </summary>
        BigButton,
        /// <summary>
        /// The left shoulder button (not to be confused with the 
        /// <see cref="GamepadAxis.LeftTrigger"/>, which is located right 
        /// below on most gamepads).
        /// </summary>
        LeftShoulder,
        /// <summary>
        /// The left analog control stick, when pushed "inwards".
        /// </summary>
        LeftStick,
        /// <summary>
        /// The right shoulder button (not to be confused with the 
        /// <see cref="GamepadAxis.RightTrigger"/>, which is located right 
        /// below on most gamepads).
        /// </summary>
        RightShoulder,
        /// <summary>
        /// The right analog control stick, when pushed "inwards".
        /// </summary>
        RightStick,
        /// <summary>
        /// The "up" button on the D-Pad.
        /// </summary>
        DPadUp,
        /// <summary>
        /// The "right" button on the D-Pad.
        /// </summary>
        DPadRight,
        /// <summary>
        /// The "down" button on the D-Pad.
        /// </summary>
        DPadDown,
        /// <summary>
        /// The "left" button on the D-Pad.
        /// </summary>
        DPadLeft
    }

    /// <summary>
    /// Defines the different triggers, sticks and their directions on
    /// a gamepad.
    /// </summary>
    public enum GamepadAxis
    {
        /// <summary>
        /// Defines the default element, which - when queried - will always 
        /// return 0.
        /// </summary>
        None,
        /// <summary>
        /// The left analog trigger (not to be confused with the 
        /// <see cref="GamepadButton.LeftShoulder"/>, which is located right
        /// above on most gamepads).
        /// </summary>
        LeftTrigger,
        /// <summary>
        /// The right analog trigger (not to be confused with the 
        /// <see cref="GamepadButton.RightShoulder"/>, which is located right
        /// above on most gamepads).
        /// </summary>
        RightTrigger,
        /// <summary>
        /// The left analog stick pushed into the left direction.
        /// If the same stick is pushed into the opposite direction
        /// (see <see cref="LeftStickRight"/>), the value of this axis is 
        /// defined to be 0.0.
        /// </summary>
        LeftStickLeft,
        /// <summary>
        /// The left analog stick pushed into the right direction.
        /// If the same stick is pushed into the opposite direction
        /// (see <see cref="LeftStickLeft"/>), the value of this axis is 
        /// defined to be 0.0.
        /// </summary>
        LeftStickRight,
        /// <summary>
        /// The left analog stick pushed into the upwards direction.
        /// If the same stick is pushed into the opposite direction
        /// (see <see cref="LeftStickDown"/>), the value of this axis is 
        /// defined to be 0.0.
        /// </summary>
        LeftStickUp,
        /// <summary>
        /// The left analog stick pushed into the downwards direction.
        /// If the same stick is pushed into the opposite direction
        /// (see <see cref="LeftStickUp"/>), the value of this axis is 
        /// defined to be 0.0.
        /// </summary>
        LeftStickDown,
        /// <summary>
        /// The right analog stick pushed into the left direction.
        /// If the same stick is pushed into the opposite direction
        /// (see <see cref="RightStickRight"/>), the value of this axis is 
        /// defined to be 0.0.
        /// </summary>
        RightStickLeft,
        /// <summary>
        /// The right analog stick pushed into the right direction.
        /// If the same stick is pushed into the opposite direction
        /// (see <see cref="RightStickLeft"/>), the value of this axis is 
        /// defined to be 0.0.
        /// </summary>
        RightStickRight,
        /// <summary>
        /// The right analog stick pushed into the upwards direction.
        /// If the same stick is pushed into the opposite direction
        /// (see <see cref="RightStickDown"/>), the value of this axis is 
        /// defined to be 0.0.
        /// </summary>
        RightStickUp,
        /// <summary>
        /// The right analog stick pushed into the downwards direction.
        /// If the same stick is pushed into the opposite direction
        /// (see <see cref="RightStickUp"/>), the value of this axis is 
        /// defined to be 0.0.
        /// </summary>
        RightStickDown
    }

    /// <summary>
    /// Defines the buttons on a mouse.
    /// </summary>
    public enum MouseButton
    {
        /// <summary>
        /// Defines the default element, which - when queried - will always 
        /// return false.
        /// </summary>
        None,
        Left, Middle, Right,
        Extra1, Extra2, Extra3
    }

    /// <summary>
    /// Defines the directions a mouse can be moved into.
    /// </summary>
    public enum MouseSpeedAxis
    {
        Up, Down, Left, Right, WheelUp, WheelDown
    }
}
