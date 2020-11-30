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
    /// Provides a dummy implementation of the <see cref="IControls"/> 
    /// interface with no functionality.
    /// </summary>
    internal class ControlsDummy : IControls
    {
        public bool SupportsKeyboard => false;

        public bool SupportsMouse => false;

        public bool SupportsAccelerometer => false;

        public bool SupportsGyroscope => false;

        public bool SupportsTouch => false;

        public bool SupportsGamepads => false;

        public int AvailableGamepadsCount => 0;

        public Vector3 GetAccelerometer()
        {
            return Vector3.Zero;
        }

        public float GetGamepadAxis(GamepadAxis axis, int gamepadIndex)
        {
            return 0;
        }

        public Vector3 GetGyroscope()
        {
            return Vector3.Zero;
        }

        public Vector2 GetMousePosition()
        {
            return Vector2.Zero;
        }

        public float GetMouseSpeed(MouseSpeedAxis axis)
        {
            return 0;
        }

        public string GetTypedCharacters()
        {
            return string.Empty;
        }

        public bool IsPressed(KeyboardKey button)
        {
            return false;
        }

        public bool IsPressed(MouseButton button)
        {
            return false;
        }

        public bool IsPressed(GamepadButton button, int gamepadIndex)
        {
            return false;
        }

        public bool IsTouched(int touchPoint, out Vector2 position)
        {
            position = Vector2.Zero;
            return false;
        }

        public void SetMouse(MouseMode mode)
        {
            return;
        }
    }
}
