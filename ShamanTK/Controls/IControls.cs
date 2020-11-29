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

using System.Numerics;

namespace ShamanTK.Controls
{
    /// <summary>
    /// Represents the unit of the platform which provides access to user 
    /// input via different input devices.
    /// </summary>
    /// <remarks>
    /// If devices are removed or plugged in during the lifetime of an 
    /// <see cref="IControls"/> instance, these changes should be picked up and
    /// be made available to the instance - but it's not required and shouldn't
    /// be expected.
    /// It is expected that the input values, which can be requested with the
    /// methods of <see cref="IControls"/> 
    /// (like <see cref="IsPressed(KeyboardKey)"/>, <see cref="GetMouseSpeed"/>
    /// or <see cref="GetTypedCharacters"/>) reflect the state at the 
    /// beginning of the update cycle of the <see cref="Graphics.IGraphics"/>
    /// unit in the same <see cref="ShamanApp"/> and don't change while the
    /// update event is still in progress. This expectation is important
    /// especially for <see cref="GetTypedCharacters"/>.
    /// Changes in the <see cref="MouseMode"/> musn't have an influence on the
    /// values of <see cref="GetMouseSpeed(MouseSpeedAxis)"/> - depending on
    /// the implementation details, the first invocation of 
    /// <see cref="GetMouseSpeed(MouseSpeedAxis)"/> with 
    /// <see cref="MouseMode.InvisibleFixed"/> could return an incorrectly
    /// high value, which needs to be adressed in the implementation.
    /// </remarks>
    public interface IControls
    {
        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a keyboard (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        bool SupportsKeyboard { get; }

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a mouse (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        bool SupportsMouse { get; }

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a accelerometer (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        bool SupportsAccelerometer { get; }

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a gyroscope (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        bool SupportsGyroscope { get; }

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a touch screen (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        bool SupportsTouch { get; }

        /// <summary>
        /// Gets a value indicating whether the current control unit supports
        /// access to a gamepad (<c>true</c>) or not (<c>false</c>).
        /// This value doesn't change during the lifetime of the current 
        /// instance.
        /// </summary>
        bool SupportsGamepads { get; }

        /// <summary>
        /// Gets the amount of currently connected gamepads or 0, if no 
        /// gamepads are available or <see cref="SupportsGamepads"/> is
        /// <c>false</c>.
        /// </summary>
        int AvailableGamepadsCount { get; }

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
        bool IsPressed(KeyboardKey button);

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
        bool IsPressed(MouseButton button);

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
        bool IsPressed(GamepadButton button, int gamepadIndex);

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
        bool IsTouched(int touchPoint, out Vector2 position);

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
        /// <remarks>
        /// See the <see cref="GamepadAxis"/> enum for more informations on
        /// which values are expected to be retrieved in which cases.
        /// </remarks>
        float GetGamepadAxis(GamepadAxis axis, int gamepadIndex);

        /// <summary>
        /// Gets the current accerlation of the mouse in a certain direction.
        /// </summary>
        /// <param name="axis">
        /// The axis which should be queried.
        /// </param>
        /// <returns>
        /// The returned <see cref="float"/> will be 0.0 if the mouse isn't
        /// moved or <see cref="SupportsMouse"/> is <c>true</c>. A value 
        /// greater than 0.0 and smaller than 1.0 specifies a normal movement 
        /// in the ranges that could also be reached by pushing a key, button 
        /// or a gamepad axis fully into one direction. Values greater than
        /// 1.0 indicate a faster movement.
        /// </returns>
        /// <remarks>
        /// When implemented, this method should behave in a similar way like 
        /// the <see cref="GetGamepadAxis(GamepadAxis, int)"/> method, so that
        /// an application using both values could be controlled with a similar
        /// "feeling" (e.g. mouse look, cursor movement, etc).
        /// </remarks>
        float GetMouseSpeed(MouseSpeedAxis axis);

        /// <summary>
        /// Gets the current mouse position in relative canvas coordinates.
        /// </summary>
        /// <returns>
        /// The mouse position as <see cref="Vector2"/>, which specifies the
        /// current mouse position in relative canvas coordinates
        /// with its origin in the upper left corner of the window/canvas
        /// and component values between 0.0 and 1.0.
        /// </returns>
        Vector2 GetMousePosition();

        /// <summary>
        /// Gets the current value of the accelerometer as normalized
        /// <see cref="Vector3"/>.
        /// </summary>
        /// <returns>
        /// A new instance of the <see cref="Vector3"/> struct or
        /// <see cref="Vector3.Zero"/>, when 
        /// <see cref="SupportsAccelerometer"/> is <c>false</c>.
        /// </returns>
        Vector3 GetAccelerometer();

        /// <summary>
        /// Gets the current value of the gyroscope as normalized
        /// <see cref="Vector3"/>.
        /// </summary>
        /// <returns>
        /// A new instance of the <see cref="Vector3"/> struct or
        /// <see cref="Vector3.Zero"/>, when 
        /// <see cref="SupportsGyroscope"/> is <c>false</c>.
        /// </returns>
        Vector3 GetGyroscope();

        /// <summary>
        /// Gets the characters typed on the keyboard since the last update,
        /// using the currently active keyboard layout. This does not include
        /// any control characters beside whitespaces.
        /// </summary>
        /// <returns>
        /// A string of typed characters or an empty string, if no characters
        /// were typed or <see cref="SupportsKeyboard"/> is <c>false</c>.
        /// </returns>
        string GetTypedCharacters();

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
        void SetMouse(MouseMode mode);
    }
}
