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
using System.Numerics;
using Eterra.IO;

namespace Eterra.Sound
{
    /// <summary>
    /// Provides a dummy implementation of the <see cref="ISound"/> 
    /// interface with no functionality.
    /// </summary>
    internal class SoundDummy : ISound
    {
        private class SoundSourceDummy : SoundSource
        {
            private readonly bool disposeSource;
            private readonly SoundDataStream soundDataStream;

            public SoundSourceDummy(bool disposeSource, 
                SoundDataStream soundDataStream) : base()
            {
                this.disposeSource = disposeSource;
                this.soundDataStream = soundDataStream;
            }

            public override TimeSpan PlaybackPosition => TimeSpan.Zero;

            public override bool IsPlaying => false;

            public override bool IsBuffering => false;

            public override float Volume
            {
                get => volume;
                set => volume = volume = Math.Max(Math.Min(0, value), 1);
            }
            private float volume = 1;
            public override void Pause() { return; }

            public override void Play(bool loop) { return; }

            public override void Stop() { return; }

            protected override void Dispose(bool disposing)
            {
                if (disposing && disposeSource) soundDataStream.Dispose();
            }
        }

        public Vector3 ListenerLocation { get; set; }

        public SoundSource CreateSoundSource(SoundDataStream soundDataStream,
            bool disposeSource)
        {
            if (soundDataStream == null)
                throw new ArgumentNullException(nameof(soundDataStream));

            return new SoundSourceDummy(disposeSource, soundDataStream);
        }

        public void Dispose() { return; }
    }
}
