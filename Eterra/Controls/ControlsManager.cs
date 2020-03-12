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
using System.Collections.Generic;
#if ENABLE_EXPERIMENTAL_API
using Eterra.Common;
using System.Linq;
using System.Threading.Tasks;
#endif

namespace Eterra.Controls
{
    /// <summary>
    /// Gets the value of a certain human interface device property.
    /// </summary>
    /// <typeparam name="T">
    /// The value type which the input value is expected to have.
    /// </typeparam>
    /// <param name="controls">
    /// The <see cref="IControls"/> unit from which the value should be taken.
    /// </param>
    /// <returns>
    /// A new instance of the type <typeparamref name="T"/>.
    /// </returns>
    public delegate T ControllerElementUpdater<T>(IControls controls);

    /// <summary>
    /// Provides a manager for mapping the events and values from various 
    /// human interface devices to an application-specific role.
    /// </summary>
    public class ControlsManager
    {
#if ENABLE_EXPERIMENTAL_API
        //This concept hasn't been tested once and lacks the functionality of
        //mapping two different devices (e.g. keyboard and gamepad) to one
        //ControlMapping.
        private class MappingSyncTask : SyncTask<ControlMapping>
        {
            private static readonly int keyboardKeysCount = (int)Enum
                .GetValues(typeof(KeyboardKey)).Cast<KeyboardKey>().Max();
            private static readonly int mouseButtonsCount = (int)Enum
                .GetValues(typeof(MouseButton)).Cast<MouseButton>().Max();
            private static readonly int gamepadButtonsCount = (int)Enum
                .GetValues(typeof(GamepadButton)).Cast<GamepadButton>().Max();
            private static readonly int gamepadAxisCount = (int)Enum
                .GetValues(typeof(GamepadAxis)).Cast<GamepadAxis>().Max();

            private readonly bool[] keyboardKeysPressed, mouseButtonsPressed;
            private readonly bool[][] gamepadButtonsPressed;
            private readonly float[][] gamepadAxisValues;

            private readonly IControls controls;
            private readonly DateTime timeout;

            public MappingSyncTask(IControls controls, TimeSpan timeout)
                : base("ControlMapping")
            {
                this.controls = controls ??
                    throw new ArgumentNullException(nameof(controls));

                int gamepadCount = controls.AvailableGamepadsCount;

                keyboardKeysPressed = new bool[keyboardKeysCount];
                mouseButtonsPressed = new bool[mouseButtonsCount];
                gamepadButtonsPressed = new bool[gamepadCount][];
                gamepadAxisValues = new float[gamepadCount][];
                for (int g = 0; g < gamepadCount; g++)
                {
                    gamepadButtonsPressed[g] = new bool[gamepadButtonsCount];
                    gamepadAxisValues[g] = new float[gamepadAxisCount];
                }

                UpdateKeyboardKeys(false);
                UpdateMouseButtons(false);
                UpdateGamepadButtons(false);
                UpdateGamepadAxis(false);

                this.timeout = DateTime.Now + timeout;
            }

            private bool abortFlag = false;

            public void Abort()
            {
                abortFlag = true;
            }

            private KeyboardKey UpdateKeyboardKeys(bool returnFirstMatch)
            {
                KeyboardKey changedKey = KeyboardKey.None;
                for (int i = 0; i < keyboardKeysCount; i++)
                {
                    KeyboardKey currentKey = (KeyboardKey)i;
                    bool oldValue = keyboardKeysPressed[i];
                    bool currentValue =
                        controls.IsPressed(currentKey);
                    if (oldValue != currentValue)
                    {
                        changedKey = currentKey;
                        if (returnFirstMatch) return changedKey;
                    }
                    keyboardKeysPressed[i] = currentValue;
                }
                return changedKey;
            }

            private MouseButton UpdateMouseButtons(bool returnFirstMatch)
            {
                MouseButton changedButton = MouseButton.None;
                for (int i = 0; i < mouseButtonsCount; i++)
                {
                    MouseButton currentButton = (MouseButton)i;
                    bool oldValue = mouseButtonsPressed[i];
                    bool currentValue =
                        controls.IsPressed(currentButton);
                    if (oldValue != currentValue)
                    {
                        changedButton = currentButton;
                        if (returnFirstMatch) return changedButton;
                    }
                    mouseButtonsPressed[i] = currentValue;
                }
                return changedButton;
            }

            private Tuple<int, GamepadButton> UpdateGamepadButtons(
                bool returnFirstMatch)
            {
                Tuple<int, GamepadButton> changedButton =
                    new Tuple<int, GamepadButton>(0, GamepadButton.None);
                for (int g = 0; g < gamepadButtonsPressed.Length; g++)
                {
                    for (int i = 0; i < gamepadButtonsCount; i++)
                    {
                        GamepadButton currentButton = (GamepadButton)i;
                        bool oldValue = gamepadButtonsPressed[g][i];
                        bool currentValue =
                            controls.IsPressed(currentButton, g);
                        if (oldValue != currentValue)
                        {
                            changedButton = new Tuple<int, GamepadButton>(g,
                                currentButton);
                            if (returnFirstMatch) return changedButton;
                        }
                        gamepadButtonsPressed[g][i] = currentValue;
                    }
                }
                return changedButton;
            }

            private Tuple<int, GamepadAxis> UpdateGamepadAxis(
                bool returnFirstMatch)
            {
                Tuple<int, GamepadAxis> changedAxis =
                    new Tuple<int, GamepadAxis>(0, GamepadAxis.None);
                for (int g = 0; g < gamepadAxisValues.Length; g++)
                {
                    for (int i = 0; i < gamepadAxisCount; i++)
                    {
                        GamepadAxis currentAxis = (GamepadAxis)i;
                        float oldValue = gamepadAxisValues[g][i];
                        float currentValue =
                            controls.GetGamepadAxis(currentAxis, g);
                        if (oldValue != currentValue)//TODO: Add treshold!
                        {
                            changedAxis = new Tuple<int, GamepadAxis>(g,
                                currentAxis);
                            if (returnFirstMatch) return changedAxis;
                        }
                        gamepadAxisValues[g][i] = currentValue;
                    }
                }
                return changedAxis;
            }

            protected override ControlMapping ContinueGenerator()
            {
                if (DateTime.Now > timeout)
                    throw new TimeoutException("No user input was " +
                        "registered before the timeout.");
                if (abortFlag)
                    throw new TaskCanceledException("The task " +
                        "was cancelled.");

                KeyboardKey keyboardKey = UpdateKeyboardKeys(true);
                if (keyboardKey != KeyboardKey.None)
                    return new ControlMapping(c => c.IsPressed(keyboardKey));

                MouseButton mouseButton = UpdateMouseButtons(true);
                if (mouseButton != MouseButton.None)
                    return new ControlMapping(c => c.IsPressed(mouseButton));

                Tuple<int, GamepadButton> gamepadButton =
                    UpdateGamepadButtons(true);
                if (gamepadButton.Item2 != GamepadButton.None)
                    return new ControlMapping(c =>
                    c.IsPressed(gamepadButton.Item2, gamepadButton.Item1));

                Tuple<int, GamepadAxis> gamepadAxis =
                    UpdateGamepadAxis(true);
                if (gamepadAxis.Item2 != GamepadAxis.None)
                    return new ControlMapping(c =>
                    c.GetGamepadAxis(gamepadAxis.Item2, gamepadAxis.Item1));

                return null;
            }
        }

        /// <summary>
        /// Gets the default amount of time after which a background mapping
        /// started with <see cref="MapNextInput(string)"/> is aborted.
        /// </summary>
        public static TimeSpan MappingTimeout { get; }
            = TimeSpan.FromSeconds(5);

        private MappingSyncTask currentMappingTask = null;
#endif

        /// <summary>
        /// Gets a value indicating whether the current 
        /// <see cref="ControlsManager"/> instance manages a functional 
        /// controls unit (<c>true</c>) or if no controls unit was specified by 
        /// the platform provider upon creation and using the controls-related 
        /// functionality will have no effect (<c>false</c>).
        /// </summary>
        public bool IsFunctional => !(Input is ControlsDummy);

        /// <summary>
        /// Gets the <see cref="IControls"/> unit, which is used by the current
        /// <see cref="ControlsManager"/> to retrieve the values of the
        /// various available devices.
        /// </summary>
        public IControls Input { get; }

        private readonly List<ControlMapping> mappings 
            = new List<ControlMapping>();       

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="ControlsManager"/> class.
        /// </summary>
        /// <param name="controls">The base controls unit.</param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="controls"/> is null.
        /// </exception>
        internal ControlsManager(IControls controls)
        {
            Input = controls ??
                throw new ArgumentNullException(nameof(controls));
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(KeyboardKey primaryInput)
        {
            return AddMapping(new ControlMapping()
            {
                InputKeyboardKey = primaryInput
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(KeyboardKey primaryInput, 
            MouseButton secondaryInput)
        {
            return AddMapping(new ControlMapping()
            {
                InputKeyboardKey = primaryInput,
                InputMouseButton = secondaryInput
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="secondaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(KeyboardKey primaryInput,
            GamepadButton secondaryInput, int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputKeyboardKey = primaryInput,
                InputGamepadButton = secondaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="secondaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(KeyboardKey primaryInput,
            GamepadAxis secondaryInput, int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputKeyboardKey = primaryInput,
                InputGamepadAxis = secondaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="tertiaryInput">
        /// The tertiary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="secondaryInput"/> and the
        /// <paramref name="tertiaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(KeyboardKey primaryInput,
            GamepadButton secondaryInput, GamepadAxis tertiaryInput,
            int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputKeyboardKey = primaryInput,
                InputGamepadButton = secondaryInput,
                InputGamepadAxis = tertiaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="tertiaryInput">
        /// The tertiary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="tertiaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(KeyboardKey primaryInput,
            MouseButton secondaryInput, GamepadButton tertiaryInput,
            int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputKeyboardKey = primaryInput,
                InputMouseButton = secondaryInput,
                InputGamepadButton = tertiaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="tertiaryInput">
        /// The tertiary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="tertiaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(KeyboardKey primaryInput,
            MouseButton secondaryInput, GamepadAxis tertiaryInput,
            int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputKeyboardKey = primaryInput,
                InputMouseButton = secondaryInput,
                InputGamepadAxis = tertiaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="tertiaryInput">
        /// The tertiary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="quaterniaryInput">
        /// The quaterniary input element, (non-exclusively) assigned to the 
        /// new <see cref="ControlMapping"/>. And yes, I had to look that word
        /// up.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="tertiaryInput"/> and the
        /// <paramref name="quaterniaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(KeyboardKey primaryInput,
            MouseButton secondaryInput, GamepadButton tertiaryInput,
            GamepadAxis quaterniaryInput, int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputKeyboardKey = primaryInput,
                InputMouseButton = secondaryInput,
                InputGamepadButton = tertiaryInput,
                InputGamepadAxis = quaterniaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(KeyboardKey primaryInput,
            MouseSpeedAxis secondaryInput)
        {
            return AddMapping(new ControlMapping()
            {
                InputKeyboardKey = primaryInput,
                InputMouseSpeed = secondaryInput
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="tertiaryInput">
        /// The tertiary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="tertiaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(KeyboardKey primaryInput,
            MouseSpeedAxis secondaryInput, GamepadButton tertiaryInput,
            int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputKeyboardKey = primaryInput,
                InputMouseSpeed = secondaryInput,
                InputGamepadButton = tertiaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="tertiaryInput">
        /// The tertiary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="secondaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(KeyboardKey primaryInput,
            MouseSpeedAxis secondaryInput, GamepadAxis tertiaryInput,
            int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputKeyboardKey = primaryInput,
                InputMouseSpeed = secondaryInput,
                InputGamepadAxis = tertiaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="tertiaryInput">
        /// The tertiary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="quaterniaryInput">
        /// The quaterniary input element, (non-exclusively) assigned to the 
        /// new <see cref="ControlMapping"/>. And yes, I had to look that word
        /// up.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="tertiaryInput"/> and the
        /// <paramref name="quaterniaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(KeyboardKey primaryInput,
            MouseSpeedAxis secondaryInput, GamepadButton tertiaryInput,
            GamepadAxis quaterniaryInput, int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputKeyboardKey = primaryInput,
                InputMouseSpeed = secondaryInput,
                InputGamepadButton = tertiaryInput,
                InputGamepadAxis = quaterniaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(MouseButton primaryInput)
        {
            return AddMapping(new ControlMapping()
            {
                InputMouseButton = primaryInput
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="secondaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(MouseButton primaryInput, 
            GamepadButton secondaryInput, int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputMouseButton = primaryInput,
                InputGamepadButton = secondaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="tertiaryInput">
        /// The tertiary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="secondaryInput"/> and the 
        /// <paramref name="tertiaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(MouseButton primaryInput,
            GamepadButton secondaryInput, GamepadAxis tertiaryInput, 
            int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputMouseButton = primaryInput,
                InputGamepadButton = secondaryInput,
                InputGamepadAxis = tertiaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="secondaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(MouseButton primaryInput,
            GamepadAxis secondaryInput, int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputMouseButton = primaryInput,
                InputGamepadAxis = secondaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(MouseSpeedAxis primaryInput)
        {
            return AddMapping(new ControlMapping()
            {
                InputMouseSpeed = primaryInput
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="secondaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(MouseSpeedAxis primaryInput,
            GamepadButton secondaryInput, int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputMouseSpeed = primaryInput,
                InputGamepadButton = secondaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="secondaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(MouseSpeedAxis primaryInput,
            GamepadAxis secondaryInput, int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputMouseSpeed = primaryInput,
                InputGamepadAxis = secondaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="primaryInput">
        /// The primary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="secondaryInput">
        /// The secondary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="tertiaryInput">
        /// The tertiary input element, (non-exclusively) assigned to the new 
        /// <see cref="ControlMapping"/>.
        /// </param>
        /// <param name="gamepadIndex">
        /// The index of the gamepad used as source for the
        /// <paramref name="secondaryInput"/> and the 
        /// <paramref name="tertiaryInput"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="ControlMapping"/> instance, which value is the
        /// current maximum of the mapped input elements.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when one of the specified enum values is invalid.
        /// </exception>
        public ControlMapping Map(MouseSpeedAxis primaryInput,
            GamepadButton secondaryInput, GamepadAxis tertiaryInput,
            int gamepadIndex = 0)
        {
            return AddMapping(new ControlMapping()
            {
                InputMouseSpeed = primaryInput,
                InputGamepadButton = secondaryInput,
                InputGamepadAxis = tertiaryInput,
                InputGamepadIndex = gamepadIndex
            });
        }

        /// <summary>
        /// Creates a new custom <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="elementUpdater">
        /// The function delegate which returns the current value for the
        /// mapping, when invoked.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="ControlMapping"/> class.
        /// </returns>
        public ControlMapping MapCustom(
            ControllerElementUpdater<float> elementUpdater)
        {
            if (elementUpdater == null)
                throw new ArgumentNullException(nameof(elementUpdater));

            return AddMapping(new ControlMapping()
            {
                InputCustom = elementUpdater
            });
        }

        /// <summary>
        /// Creates a new custom <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="elementUpdater">
        /// The function delegate which returns the current value for the
        /// mapping, when invoked.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="ControlMapping"/> class.
        /// </returns>
        public ControlMapping MapCustom(
            ControllerElementUpdater<bool> elementUpdater)
        {
            if (elementUpdater == null)
                throw new ArgumentNullException(nameof(elementUpdater));

            return AddMapping(new ControlMapping()
            {
                InputCustom = c => elementUpdater(c) ? 1 : 0
            });
        }

        private ControlMapping AddMapping(ControlMapping controlMapping)
        {
            if (controlMapping == null)
                throw new ArgumentNullException(nameof(controlMapping));

            mappings.Add(controlMapping);

            return controlMapping;
        }

#if ENABLE_EXPERIMENTAL_API
#warning Unstable control API elements enabled - use with caution!
        /// <summary>
        /// Begins listening for events on the keyboard, mouse buttons,
        /// gamepad buttons or gamepad axis in <see cref="Input"/> and
        /// binds the first changed input parameter to a new 
        /// <see cref="ControlMapping"/>-
        /// The <see cref="SyncTask{T}"/> is aborted after the timeout
        /// specified by <see cref="MappingTimeout"/>.
        /// </summary>
        /// <returns>
        /// A new instance of the <see cref="SyncTask{ControlMapping}"/> class,
        /// which encapsulates the mapping process and will either contain
        /// the created or updated <see cref="ControlMapping"/> when the
        /// task completed successfully (the user did some sort of input on
        /// one of the supported devices) or the description about what caused
        /// the task to fail (timeout, aborted because another mapping task
        /// was started).
        /// </returns>
        /// <remarks>
        /// If a mapping task is currently waiting for user input and another
        /// task is started, the previous one will be aborted.
        /// </remarks>
        public SyncTask<ControlMapping> MapNextInput()
        {
            return MapNextInput(MappingTimeout);
        }

        /// <summary>
        /// Begins listening for events on the keyboard, mouse buttons,
        /// gamepad buttons or gamepad axis in <see cref="Input"/> and
        /// binds the first changed input parameter to a new 
        /// <see cref="ControlMapping"/>.
        /// </summary>
        /// <param name="timeout">
        /// The amount of time after which the task is aborted.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="SyncTask{ControlMapping}"/> class,
        /// which encapsulates the mapping process and will either contain
        /// the created or updated <see cref="ControlMapping"/> when the
        /// task completed successfully (the user did some sort of input on
        /// one of the supported devices) or the description about what caused
        /// the task to fail (timeout, aborted because another mapping task
        /// was started).
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="timeout"/> is less than
        /// <see cref="TimeSpan.Zero"/>.
        /// </exception>
        /// <remarks>
        /// If a mapping task is currently waiting for user input and another
        /// task is started, the previous one will be aborted.
        /// </remarks>
        public SyncTask<ControlMapping> MapNextInput(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            if (currentMappingTask != null &&
                currentMappingTask.CurrentState != SyncTaskState.Finished &&
                currentMappingTask.CurrentState != SyncTaskState.Failed)
            {
                currentMappingTask.Abort();
                currentMappingTask.Continue(new TimeSpan(0, 0, 1));
                currentMappingTask = null;
            }

            currentMappingTask = new MappingSyncTask(Input, timeout);
            currentMappingTask.Completed += delegate (object sender,
                SyncTaskCompletedEventArgs<ControlMapping> args)
            {
                if (args.Success) Map(args.Result);
            };

            return currentMappingTask;
        }
#endif

        /// <summary>
        /// Removes a mapping from the current 
        /// <see cref="ControlsManager"/> and stops updating it.
        /// </summary>
        /// <param name="mapping">
        /// The mapping to be removed.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="mapping"/> was found 
        /// and removed from this <see cref="ControlsManager"/> instance, 
        /// <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="mapping"/> is null.
        /// </exception>
        public bool Unmap(ControlMapping mapping)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            return mappings.Remove(mapping);
        }

        /// <summary>
        /// Updates the current <see cref="ControlsManager"/> instance
        /// and all <see cref="ControlMapping"/> instances managed by it.
        /// </summary>
        /// <param name="delta">The time elapsed since the last update.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="delta"/> is less than 
        /// <see cref="TimeSpan.Zero"/>.
        /// </exception>
        internal void Update(TimeSpan delta)
        {
            if (delta < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(delta));

#if ENABLE_EXPERIMENTAL_API
            if (currentMappingTask != null && 
                (currentMappingTask.CurrentState == SyncTaskState.Idle 
                || currentMappingTask.CurrentState == SyncTaskState.Running))
                currentMappingTask.Continue(new TimeSpan(0, 0, 1));
#endif

            foreach (ControlMapping mapping in mappings)
                mapping.Update(Input, delta);
        }
    }
}
