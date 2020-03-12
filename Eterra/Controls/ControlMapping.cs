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

using System;

namespace Eterra.Controls
{
    /// <summary>
    /// Provides a semantic access to a certain human interface device
    /// property.
    /// </summary>
    public class ControlMapping
    {
#if ENABLE_EXPERIMENTAL_API
        //This smoothing algorithm produces good results for smoothing mouse
        //movements, but it's horribly inefficient and could probably be 
        //implemented much simpler - the approach in MathHelper.Accerlate
        //looks promising.

        /// <summary>
        /// Defines the minimum smoothing strength.
        /// </summary>
        public const int SmoothingIntensityMin = 1;

        /// <summary>
        /// Defines the maximum smoothing strength.
        /// </summary>
        public const int SmoothingIntensityMax = 15;
#endif

        /// <summary>
        /// Defines the treshold which needs to be exceeded by 
        /// <see cref="Value"/> to be considered active.
        /// </summary>
        public const float ActivationTreshold = 0.45f;

#if ENABLE_EXPERIMENTAL_API
        private readonly float[] valueHistory 
            = new float[SmoothingIntensityMax];
        private int currentValueSlot = 0;
#endif

        /// <summary>
        /// Gets the original value of the current 
        /// <see cref="ControlMapping"/>.
        /// </summary>
        public float Value { get; private set; }

        /// <summary>
        /// Gets the accerlation of the 
        /// current <see cref="ControlMapping"/>.
        /// </summary>
        public float Accerlation { get; private set; }

        /// <summary>
        /// Gets a boolean indicating whether the current 
        /// <see cref="ControlMapping"/> is currently active (<c>true</c>)
        /// or not (<c>false</c>).
        /// </summary>
        public bool IsActive
        {
            get => Value >= ActivationTreshold; // Math.Abs(Value) >= (1 - ToggleTreshold);
        }

        /// <summary>
        /// Gets a boolean indicating whether the current 
        /// <see cref="ControlMapping"/> has been activated during the 
        /// last update (<c>true</c>) or not (<c>false</c>), e.g. when a button
        /// is pressed for the first time.
        /// </summary>
        /// <remarks>
        /// The state of the <see cref="ControlMapping"/> is defined as 
        /// activated if the current accerlation is greater or 
        /// equal to the defined toggle treshold delta value.
        /// In contrast to the <see cref="IsActive"/> value, 0 is used 
        /// as starting point of the range to allow non-binary control
        /// elements (like joysticks, etc.) to be picked up as activated
        /// without having the user "accerlate" the element too vigorously.
        /// </remarks>
        public bool IsActivated
        {
            get => Value > ActivationTreshold && 
                (Value - Math.Max(0, Accerlation)) <= ActivationTreshold;
        }

        /// <summary>
        /// Gets a boolean indicating whether the current 
        /// <see cref="ControlMapping"/> has been deactivated during the 
        /// last update (<c>true</c>) or not (<c>false</c>), e.g. when a button
        /// is pressed and then released.
        /// </summary>
        /// <remarks>
        /// The state of the <see cref="ControlMapping"/> is defined as 
        /// deactivated if the current accerlation is less or 
        /// equal to zero minus the defined toggle treshold delta value.
        /// In contrast to the <see cref="IsActive"/> value, 0 is used 
        /// as starting point of the range to allow non-binary control
        /// elements (like joysticks, etc.) to be picked up as activated
        /// without having the user "accerlate" the element too vigorously.
        /// </remarks>
        public bool IsDeactivated
        {
            get => Value < ActivationTreshold &&
                (Value - Math.Min(Accerlation, 0)) >= ActivationTreshold;
        }

        /// <summary>
        /// Gets a boolean indicating whether the current 
        /// <see cref="ControlMapping"/> value is inactive (<c>true</c>)
        /// or not (<c>false</c>).
        /// </summary>
        /// <remarks>
        /// The state of the <see cref="ControlMapping"/> is defined as 
        /// inactive if the current (absolute) value is less or equal the
        /// defined toggle treshold delta value.
        /// </remarks>
        public bool IsInactive
        {
            get => Value <= ActivationTreshold;
        }

        /// <summary>
        /// Gets or sets a <see cref="KeyboardKey"/> whose value is used 
        /// in combination with the values of the other input element sources 
        /// to update the <see cref="Value"/> and <see cref="Accerlation"/>. 
        /// Can be null.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified value is no valid element of the
        /// enumeration type.
        /// </exception>
        internal KeyboardKey? InputKeyboardKey
        {
            get => inputKeyboardKey;
            set
            {
                if (!value.HasValue ||
                    Enum.IsDefined(typeof(KeyboardKey), value.Value))
                    inputKeyboardKey = value;
                else throw new ArgumentException("The specified value is " +
                    "no valid " + nameof(KeyboardKey) + " value.");
            }
        }
        private KeyboardKey? inputKeyboardKey;

        /// <summary>
        /// Gets or sets a <see cref="MouseButton"/> whose value is used 
        /// in combination with the values of the other input element sources 
        /// to update the <see cref="Value"/> and <see cref="Accerlation"/>. 
        /// Can be null.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified value is no valid element of the
        /// enumeration type.
        /// </exception>
        internal MouseButton? InputMouseButton
        {
            get => inputMouseButton;
            set
            {
                if (!value.HasValue ||
                    Enum.IsDefined(typeof(MouseButton), value.Value))
                    inputMouseButton = value;
                else throw new ArgumentException("The specified value is " +
                    "no valid " + nameof(MouseButton) + " value.");
            }
        }
        private MouseButton? inputMouseButton;

        /// <summary>
        /// Gets or sets a <see cref="MouseSpeedAxis"/> whose value is used 
        /// in combination with the values of the other input element sources 
        /// to update the <see cref="Value"/> and <see cref="Accerlation"/>. 
        /// Can be null.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified value is no valid element of the
        /// enumeration type.
        /// </exception>
        internal MouseSpeedAxis? InputMouseSpeed
        {
            get => inputMouseSpeed;
            set
            {
                if (!value.HasValue ||
                    Enum.IsDefined(typeof(MouseSpeedAxis), value.Value))
                    inputMouseSpeed = value;
                else throw new ArgumentException("The specified value is " +
                    "no valid " + nameof(MouseSpeedAxis) + " value.");
            }
        }
        private MouseSpeedAxis? inputMouseSpeed;

        /// <summary>
        /// Gets or sets a <see cref="GamepadButton"/> whose value is used 
        /// in combination with the values of the other input element sources 
        /// to update the <see cref="Value"/> and <see cref="Accerlation"/>. 
        /// Can be null.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified value is no valid element of the
        /// enumeration type.
        /// </exception>
        internal GamepadButton? InputGamepadButton
        {
            get => inputGamepadButton;
            set
            {
                if (!value.HasValue ||
                    Enum.IsDefined(typeof(GamepadButton), value.Value))
                    inputGamepadButton = value;
                else throw new ArgumentException("The specified value is " +
                    "no valid " + nameof(GamepadButton) + " value.");
            }
        }
        private GamepadButton? inputGamepadButton;

        /// <summary>
        /// Gets or sets a <see cref="GamepadAxis"/> whose value is used 
        /// in combination with the values of the other input element sources 
        /// to update the <see cref="Value"/> and <see cref="Accerlation"/>. 
        /// Can be null.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified value is no valid element of the
        /// enumeration type.
        /// </exception>
        internal GamepadAxis? InputGamepadAxis
        {
            get => inputGamepadAxis;
            set
            {
                if (!value.HasValue ||
                    Enum.IsDefined(typeof(GamepadAxis), value.Value))
                    inputGamepadAxis = value;
                else throw new ArgumentException("The specified value is " +
                    "no valid " + nameof(GamepadAxis) + " value.");
            }
        }
        private GamepadAxis? inputGamepadAxis;        

        /// <summary>
        /// Gets or sets the index of the gamepad which should be queried when
        /// the values of <see cref="InputGamepadButton"/> and/or
        /// <see cref="InputGamepadAxis"/> is queried. The default value is 0.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when the assigned value is less than 0.
        /// </exception>
        internal int InputGamepadIndex
        {
            get => inputGamepadIndex;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                else inputGamepadIndex = value;
            }
        }
        private int inputGamepadIndex;

        /// <summary>
        /// Gets or sets a <see cref="KeyboardKey"/> whose value is used 
        /// in combination with the values of the other input element sources 
        /// to update the <see cref="Value"/> and <see cref="Accerlation"/>. 
        /// Can be null.
        /// </summary>
        internal ControllerElementUpdater<float> InputCustom { get; set; }
            = null;

        /// <summary>
        /// Gets a value indicating whether the <see cref="Value"/> is limited
        /// at a maximum of 1.0 (<c>true</c>) or not (<c>false</c>, default).
        /// </summary>
        public bool LimitValue { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlMapping"/>
        /// class.
        /// </summary>
        internal ControlMapping() { }

        /// <summary>
        /// Updates the state of this <see cref="ControlMapping"/> instance.
        /// </summary>
        /// <param name="controls">
        /// The controls unit to take the input device values from.
        /// </param>
        /// <param name="delta">
        /// The time elapsed since the last update.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="delta"/> is less than
        /// <see cref="TimeSpan.Zero"/>.
        /// </exception>
        internal void Update(IControls controls, in TimeSpan delta)
        {
            if (controls == null)
                throw new ArgumentNullException(nameof(controls));
            if (delta < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(delta));

            float value = 0;

            if (InputKeyboardKey.HasValue)
                value = Math.Max(value,
                    controls.IsPressed(InputKeyboardKey.Value) ? 1 : 0);
            if (InputMouseButton.HasValue)
                value = Math.Max(value,
                    controls.IsPressed(InputMouseButton.Value) ? 1 : 0);
            if (InputGamepadButton.HasValue)
                value = Math.Max(value, controls.IsPressed(
                    InputGamepadButton.Value, InputGamepadIndex) ? 1 : 0);
            if (InputMouseSpeed.HasValue)
                value = Math.Max(value,
                    controls.GetMouseSpeed(InputMouseSpeed.Value));
            if (InputGamepadAxis.HasValue)
                value = Math.Max(value, controls.GetGamepadAxis(
                    InputGamepadAxis.Value, InputGamepadIndex));
            if (InputCustom != null)
                try { value = Math.Max(value, InputCustom(controls)); }
                catch { InputCustom = null; }

            if (LimitValue)
                value = Math.Max(Math.Min(1, value), 0);
            else value = Math.Max(value, 0);

            Accerlation = value - Value;
            Value = value;

#if ENABLE_EXPERIMENTAL_API
            currentValueSlot = (currentValueSlot + 1)
                % valueHistory.Length;
            valueHistory[currentValueSlot] = value;
            
            float valueSum = 0;
            float accerlationSum = 0;
            for (int i = 0; i < valueHistory.Length; i++)
            {
                valueSum += valueHistory[i];
                if (i > 0) accerlationSum += (valueHistory[i] -
                        valueHistory[i - 1]);
            }
#endif         
        }

#if ENABLE_EXPERIMENTAL_API
        /// <summary>
        /// Gets a smoothed value of <see cref="Value"/>.
        /// </summary>
        /// <param name="smoothingIntensity">
        /// The intensity of the smoothing - the higher, the smoother.
        /// Must be greater than/equal to <see cref="SmoothingIntensityMin"/>
        /// and less than/equal to <see cref="SmoothingIntensityMax"/>.
        /// </param>
        /// <returns>The smoothed value as <see cref="float"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="smoothingIntensity"/> is less than
        /// <see cref="SmoothingIntensityMin"/> or greater than
        /// <see cref="SmoothingIntensityMax"/>.
        /// </exception>
        public float GetValueSmoothed(int smoothingIntensity)
        {
            if (smoothingIntensity < SmoothingIntensityMin ||
                smoothingIntensity > SmoothingIntensityMax)
                throw new ArgumentOutOfRangeException(
                    nameof(smoothingIntensity));

            float valueSum = 0;
            for (int i = 0; i < smoothingIntensity; i++)
            {
                int s = (i + currentValueSlot) % valueHistory.Length;
                valueSum += valueHistory[s];
            }
            return valueSum / smoothingIntensity;
        }

        /// <summary>
        /// Gets a smoothed value of <see cref="Accerlation"/>.
        /// </summary>
        /// <param name="smoothingIntensity">
        /// The intensity of the smoothing - the higher, the smoother.
        /// Must be greater than/equal to <see cref="SmoothingIntensityMin"/>
        /// and less than/equal to <see cref="SmoothingIntensityMax"/>.
        /// </param>
        /// <returns>The smoothed value as <see cref="float"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="smoothingIntensity"/> is less than
        /// <see cref="SmoothingIntensityMin"/> or greater than
        /// <see cref="SmoothingIntensityMax"/>.
        /// </exception>
        public float GetAccerlationSmoothed (int smoothingIntensity)
        {
            if (smoothingIntensity < SmoothingIntensityMin ||
                smoothingIntensity > SmoothingIntensityMax)
                throw new ArgumentOutOfRangeException(
                    nameof(smoothingIntensity));

            float accerlationSum = 0;
            for (int i = 0; i < smoothingIntensity; i++)
            {
                int s = (i + currentValueSlot) % valueHistory.Length;
                int sMinus = (s == 0 ? valueHistory.Length - 1 : s - 1);
                if (i > 0) accerlationSum += (valueHistory[s] -
                        valueHistory[sMinus]);
            }
            return accerlationSum / valueHistory.Length;
        }
#endif

        public static implicit operator float(ControlMapping mapping)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));
            return mapping.Value;
        }
    }
}
