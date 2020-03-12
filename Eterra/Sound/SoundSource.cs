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

using Eterra.Common;
using System;

namespace Eterra.Sound
{
    /// <summary>
    /// Represents a sound source in three-dimensional space.
    /// </summary>
    public abstract class SoundSource : DisposableBase
    {
        /// <summary>
        /// Gets the length of the sound, which is played back by the
        /// current sound source, or <see cref="TimeSpan.Zero"/>, if 
        /// <see cref="IsStream"/> is <c>true</c>.
        /// </summary>
        public TimeSpan Length { get; } = TimeSpan.Zero;

        /// <summary>
        /// Gets a value indicating whether the current 
        /// <see cref="SoundSource"/> instance continuously loads the sound 
        /// data during playback and therefore has no predefined 
        /// <see cref="Length"/> (<c>true</c>) or if the current instance 
        /// has a fixed <see cref="Length"/> and completely preloads the sound
        /// data before playing the sound back.
        /// </summary>
        public bool IsStream => Length == TimeSpan.Zero;

        /// <summary>
        /// Gets the position of the playback.
        /// </summary>
        public abstract TimeSpan PlaybackPosition { get; }

        /// <summary>
        /// Gets a value indicating whether the current 
        /// <see cref="SoundSource"/> is playing (<c>true</c>) or 
        /// paused/stopped (<c>false</c>).
        /// </summary>
        public abstract bool IsPlaying { get; }

        /// <summary>
        /// Gets a value indicating whether the current 
        /// <see cref="SoundSource"/> instance is asynchronously loading the
        /// sound data into memory (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        /// <remarks>
        /// If <see cref="IsStream"/> is <c>true</c>, this property will be 
        /// <c>true</c> right after initialisation (during the initial
        /// pre-loading) and turn back to <c>true</c> when the (small) buffer 
        /// is being refilled and the original <see cref="IO.SoundDataStream"/>
        /// hasn't ended yet.
        /// If <see cref="IsStream"/> is <c>false</c>, this property is 
        /// <c>true</c> right after the instance was initialized and turn to
        /// <c>false</c> once the complete sound data from the source
        /// <see cref="IO.SoundDataStream"/> was buffered. It will not go back
        /// to <c>true</c> in that case.
        /// </remarks>
        public abstract bool IsBuffering { get; }

        /// <summary>
        /// Gets or sets the volume of the current <see cref="SoundSource"/>
        /// as a value between 0.0 and 1.0. Values outside that range are
        /// clamped automatically.
        /// </summary>
        public abstract float Volume { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundSource"/> 
        /// base class as a <see cref="SoundSource"/> that streams the sound 
        /// data in few smaller fragments continuously before and during 
        /// playback - therefore, this instance will have no defined 
        /// <see cref="Length"/>.
        /// The buffering is done asynchronously and started in this
        /// constructor, it's state can be queried using 
        /// <see cref="IsBuffering"/>.
        /// </summary>
        /// <param name="length">
        /// The length of the sound clip, which is played back by the current
        /// sound source, or <c>null</c> if no length is available or the 
        /// <see cref="SoundSource"/> should be defined as stream.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="length"/> is less than/equal to
        /// <see cref="TimeSpan.Zero"/>.
        /// </exception>
        protected SoundSource(TimeSpan? length = null)
        {
            if (length.HasValue)
            {
                if (length.Value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(length));
                else Length = length.Value;
            }
        }

        /// <summary>
        /// Starts the playback of the current 
        /// <see cref="SoundSource"/> asynchronously. This method has no 
        /// effect when <see cref="IsPlaying"/> is <c>true</c>.
        /// </summary>
        /// <param name="loop">
        /// <c>true</c> to rewind and restart the playback after the end
        /// was reached until the playback is stopped or paused, 
        /// <c>false</c> to only play back the sound once.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current <see cref="SoundSource"/> instance
        /// was disposed and can't be used anymore.
        /// </exception>
        public abstract void Play(bool loop);

        /// <summary>
        /// Pauses the playback at the current position.
        /// This method has no effect when <see cref="IsPlaying"/> is
        /// <c>false</c>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current <see cref="SoundSource"/> instance
        /// was disposed and can't be used anymore.
        /// </exception>
        public abstract void Pause();

        /// <summary>
        /// Stops the playback and sets the playback position to 0.
        /// If <see cref="IsPlaying"/> is <c>false</c>, only the playback 
        /// position is reset to 0.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current <see cref="SoundSource"/> instance
        /// was disposed and can't be used anymore.
        /// </exception>
        public abstract void Stop();
    }
}