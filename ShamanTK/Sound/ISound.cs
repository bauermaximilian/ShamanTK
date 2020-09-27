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

using ShamanTK.IO;
using System;

namespace ShamanTK.Sound
{
    /// <summary>
    /// Represents the unit of the platform which is responsible for providing
    /// sound playback.
    /// </summary>
    public interface ISound : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="SoundSource"/> instance.
        /// </summary>
        /// <param name="soundDataStream">
        /// The sound stream which should be played back by the new source.
        /// </param>
        /// <param name="disposeSource">
        /// <c>true</c> to dispose the <paramref name="soundDataStream"/> when 
        /// the new <see cref="SoundSource"/> instance is disposed, 
        /// <c>false</c> to leave the <paramref name="soundDataStream"/> as 
        /// it is.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="SoundSource"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="soundDataStream"/> is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current instance was disposed and can't
        /// be used anymore.
        /// </exception>
        SoundSource CreateSoundSource(SoundDataStream soundDataStream, 
            bool disposeSource);
    }
}
