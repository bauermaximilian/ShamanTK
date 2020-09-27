/*
 * Eterra Framework Platforms
 * Eterra platform providers for various operating systems and devices.
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Eterra.IO;
using MP3Sharp;
using System;
using System.IO;

namespace Eterra.Platforms.Windows.IO
{
    class SoundFormatHandler : IResourceFormatHandler
    {
        private class Mp3SoundData : SoundDataStream
        {
            public override TimeSpan Position => 
                GetTimeFromDecodedByteCount(decodedBytes);

            private int decodedBytes = 0;

            private readonly Stream baseStream;
            private MP3Stream soundStream;
            
            public Mp3SoundData(Stream baseStream, MP3Stream soundStream) 
                : base (soundStream.Frequency, soundStream?.ChannelCount == 2, 
                      baseStream.CanSeek)
            {
                this.baseStream = baseStream ??
                    throw new ArgumentNullException(nameof(baseStream));
                this.soundStream = soundStream ?? 
                    throw new ArgumentNullException(nameof(soundStream));
            }

            public override void Rewind()
            {
                if (baseStream.CanSeek)
                {
                    baseStream.Position = 0;
                    //I wish the soundStream.Seek method would've worked 
                    //properly, so this weird workaround would not be required.
                    //But it doesn't. So, every time a MP3 sound file is 
                    //rewinded, a new stream is created. This is probably not
                    //the best solution and should be fixed in the next
                    //revision.
                    soundStream = new MP3Stream(baseStream);
                }
                else throw new NotSupportedException("The base stream " +
                    "doesn't support seeking.");
            }

            public override int ReadSamples(byte[] buffer)
            {
                int decodedBytes = soundStream.Read(buffer, 0, buffer.Length);
                this.decodedBytes += decodedBytes;
                return decodedBytes;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    soundStream.Dispose();
                    baseStream.Dispose();
                }
            }
        }

        public bool SupportsExport(string fileExtensionLowercase)
        {
            return false;
        }

        public bool SupportsImport(string fileExtensionLowercase)
        {
            return fileExtensionLowercase == "mp3";
        }

        public void Export(object resource, ResourceManager manager, 
            ResourcePath path, bool overwrite)
        {
            throw new NotSupportedException("This extension doesn't support " +
                "MP3 encoding.");
        }

        public T Import<T>(ResourceManager manager, ResourcePath path)
        {
            Stream stream = manager.FileSystem.OpenFile(path.Path, false);
            return (T)(object)new Mp3SoundData(stream, new MP3Stream(stream));
        }
    }
}
