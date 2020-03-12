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

namespace Eterra.Sound
{
    /// <summary>
    /// Provides access to the available functions for playing sound.
    /// </summary>
    public class SoundManager
    {
        /// <summary>
        /// Gets a value indicating whether the current 
        /// <see cref="SoundManager"/> instance manages a functional sound unit 
        /// (<c>true</c>) or if no sound unit was specified by the platform 
        /// provider upon creation and using the sound-related functionality 
        /// will have no effect (<c>false</c>).
        /// </summary>
        public bool IsFunctional => !(Sound is SoundDummy);

        /// <summary>
        /// Gets the base <see cref="ISound"/> instance.
        /// </summary>
        internal ISound Sound { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundManager"/> class.
        /// </summary>
        /// <param name="sound">
        /// The base <see cref="ISound"/> instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="sound"/> is null.
        /// </exception>
        internal SoundManager(ISound sound)
        {
            Sound = sound ?? throw new ArgumentNullException(nameof(sound));
        }
    }
}
