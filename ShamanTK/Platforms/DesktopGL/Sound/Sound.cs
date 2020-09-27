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

using ShamanTK.Common;
using ShamanTK.IO;
using ShamanTK.Sound;
using OpenTK.Audio;
using System;
using OpenTK.Audio.OpenAL;

namespace ShamanTK.Platforms.DesktopGL.Sound
{
    /// <summary>
    /// Provides an implementation of <see cref="ISound"/> for OpenAL.
    /// </summary>
    class Sound : DisposableBase, ISound
    {
        private readonly ALContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sound"/> class.
        /// </summary>
        /// <param name="settings">
        /// The settings for the current unit.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="settings"/> is null.
        /// </exception>
        /// <exception cref="SystemException">
        /// Is thrown when the sound unit couldn't be initialized because
        /// the required (redist) libraries for OpenAL are missing.
        /// </exception>
        public Sound()
        {
            try
            {
                string defaultDeviceName = ALC.GetString(ALDevice.Null, 
                    AlcGetString.DefaultDeviceSpecifier);
                ALDevice defaultDevice = ALC.OpenDevice(defaultDeviceName);
                context = ALC.CreateContext(defaultDevice, 
                    new ALContextAttributes());
                ALC.MakeContextCurrent(context);
            }
            catch (Exception exc)
            {
                throw new SystemException("The OpenAL audio unit " +
                    "couldn't be initialized. Are the OpenAL libraries " +
                    "correctly installed?", exc);
            }
        }

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
        public ShamanTK.Sound.SoundSource CreateSoundSource(
            SoundDataStream soundDataStream, bool disposeSource)
        {
            ThrowIfDisposed();
            if (soundDataStream == null)
                throw new ArgumentNullException(nameof(soundDataStream));

            return new SoundSource(soundDataStream, disposeSource);
        }

        /// <summary>
        /// Suspends and disposes the current OpenAL sound context.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to suspend and dispose the current sound context,
        /// <c>false</c> to suspend it only.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing) ALC.DestroyContext(context);
        }
    }
}
