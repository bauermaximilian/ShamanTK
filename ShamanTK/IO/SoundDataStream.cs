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
using System;
using System.IO;

namespace ShamanTK.IO
{
    /// <summary>
    /// Provides the base class from which 16-bit PCM sound data streams 
    /// are derived.
    /// </summary>
    public abstract class SoundDataStream : DisposableBase
    {
        private class WavStream : SoundDataStream
        {
            public const string HeaderStart = "RIFF";
            public const string HeaderFormat = "WAVE";
            public const string FormatChunkStart = "fmt ";
            public const string FormatChunkStartAlt = "JUNK";
            public const int FormatChunkSize = 16;
            public const short FormatCompression = 1;
            public const string DataChunkStart = "data";
            public const int DataOffset = 44;

            private readonly Stream stream;
            private readonly bool disposeStream;
            private readonly long pcmDataLength;

            private bool movedToDataOffset = false;

            public override TimeSpan Position => movedToDataOffset ?
                GetTimeFromDecodedByteCount(stream.Position - DataOffset) :
                TimeSpan.Zero;

            public WavStream(Stream pcmStream, int sampleRate,
                bool isStereo, int dataLength, bool disposeStream)
                : base(sampleRate, isStereo, 
                      pcmStream != null ? pcmStream.CanSeek : false,
                      GetTimeFromDecodedByteCount(dataLength, isStereo,
                          sampleRate))
            {
                stream = pcmStream ??
                    throw new ArgumentNullException(nameof(pcmStream));
                pcmDataLength = dataLength;
                this.disposeStream = disposeStream;
            }

            public override void Rewind()
            {
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                    movedToDataOffset = false;
                }
                else throw new NotSupportedException("The base stream " +
                  "doesn't support seeking.");
            }

            public override int ReadSamples(byte[] buffer)
            {
                if (buffer == null)
                    throw new ArgumentNullException(nameof(buffer));

                if (movedToDataOffset)
                {
                    stream.Read(new byte[DataOffset], 0, DataOffset);
                    movedToDataOffset = true;
                }

                int endClamp = (int)Math.Max((buffer.Length + stream.Position) 
                    - (pcmDataLength + DataOffset), 0);

                return stream.Read(buffer, 0, buffer.Length - endClamp);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && disposeStream) stream.Dispose();
            }
        }

        /// <summary>
        /// Defines the amount of bits a single <see cref="SoundDataStream"/> 
        /// sample has (per channel).
        /// </summary>
        public const int BitsPerSample = 16;

        /// <summary>
        /// Defines the amount of channels in a <see cref="SoundDataStream"/>
        /// that is in mono.
        /// </summary>
        public const int ChannelCountMono = 1;

        /// <summary>
        /// Defines the amount of channels in a <see cref="SoundDataStream"/>
        /// that is in stereo.
        /// </summary>
        public const int ChannelCountStereo = 2;

        /// <summary>
        /// Gets the size of a single sample decoded by the current 
        /// <see cref="SoundDataStream"/>, which depends upon whether the 
        /// sound is stereo or mono.
        /// </summary>
        public int BytesPerSample => (BitsPerSample / 8) *
            (IsStereo ? ChannelCountStereo : ChannelCountMono);

        /// <summary>
        /// Gets the amount of bytes one second of the current 
        /// <see cref="SoundDataStream"/> has.
        /// </summary>
        public int BytesPerSecond => SampleRate * BytesPerSample;

        /// <summary>
        /// Gets a value indicating whether the current buffer has two
        /// interleaved channels (<c>true</c>) or if the buffer is mono and 
        /// has just one channel (<c>false</c>).
        /// </summary>
        public bool IsStereo { get; }

        /// <summary>
        /// Gets the amount of channels in the current 
        /// <see cref="SoundDataStream"/> instance.
        /// </summary>
        protected short Channels => (short)(IsStereo ? 2 : 1);

        /// <summary>
        /// Gets the sample rate (also known as sample frequency) of the 
        /// decoded samples from the current <see cref="SoundDataStream"/>
        /// instance in hertz.
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        /// Gets the length of the sound data of the current 
        /// <see cref="SoundDataStream"/> instance or 
        /// <see cref="TimeSpan.Zero"/>, if <see cref="HasLength"/>
        /// is <c>false</c>.
        /// </summary>
        public TimeSpan Length { get; } = TimeSpan.Zero;

        /// <summary>
        /// Gets the current position in the current
        /// <see cref="SoundDataStream"/>.
        /// </summary>
        public abstract TimeSpan Position { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Length"/> property
        /// contains the length of the sound data in this 
        /// <see cref="SoundDataStream"/> instance (<c>true</c>) or if the 
        /// current instance doesn't have a defined length and the property
        /// of <see cref="Length"/> returns <see cref="TimeSpan.Zero"/>
        /// instead (<c>false</c>).
        /// </summary>
        public bool HasLength { get; } = false;

        /// <summary>
        /// Gets a value indicating whether the <see cref="Position"/>
        /// of the current <see cref="SoundDataStream"/> can be reset to
        /// <see cref="TimeSpan.Zero"/> (<c>true</c>) or if any attempts to
        /// do so will cause a <see cref="NotSupportedException"/> 
        /// (<c>false</c>).
        /// </summary>
        public bool CanRewind { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundDataStream"/>
        /// base class without a defined <see cref="Length"/>.
        /// </summary>
        /// <param name="sampleRate">
        /// The sample rate (also known as sample frequency) in hertz.
        /// </param>
        /// <param name="isStereo">
        /// <c>true</c> if the sound is in stereo, <c>false</c> if mono.
        /// </param>
        /// <param name="canRewind">
        /// <c>true</c> if the stream will support rewinding to the beginning,
        /// <c>false</c> if the position of the stream can't be modified.
        /// </param>
        /// <param name="length">
        /// The length of the sound content.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="sampleRate"/> is less than/equal 
        /// to 0 or when <paramref name="length"/> is less than/equal to 
        /// <see cref="TimeSpan.Zero"/>.
        /// </exception>
        protected SoundDataStream(int sampleRate, bool isStereo, 
            bool canRewind, TimeSpan length) : this(sampleRate, isStereo, 
                canRewind)
        {
            if (length <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(length));

            Length = length;
            HasLength = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundDataStream"/>
        /// base class without a defined <see cref="Length"/>.
        /// </summary>
        /// <param name="sampleRate">
        /// The sample rate (also known as sample frequency) in hertz.
        /// </param>
        /// <param name="isStereo">
        /// <c>true</c> if the sound is in stereo, <c>false</c> if mono.
        /// </param>
        /// <param name="canRewind">
        /// <c>true</c> if the stream will support rewinding to the beginning,
        /// <c>false</c> if the position of the stream can't be modified.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="sampleRate"/> is less than/equal 
        /// to 0.
        /// </exception>
        protected SoundDataStream(int sampleRate, bool isStereo, 
            bool canRewind)
        {
            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate));

            SampleRate = sampleRate;
            IsStereo = isStereo;
            CanRewind = canRewind;
        }

        /// <summary>
        /// Reads and decodes the sound data from the current 
        /// <see cref="SoundDataStream"/> instance into a PCM byte buffer
        /// (in the layout and size defined by <see cref="BytesPerSample"/>,
        /// <see cref="SampleRate"/> and <see cref="IsStereo"/>).
        /// </summary>
        /// <param name="buffer">
        /// The target byte buffer to put the decoded PCM data into.
        /// The size of the buffer defines the amount of bytes to be read
        /// and decoded.
        /// </param>
        /// <returns>The amount of bytes read.</returns>
        public abstract int ReadSamples(byte[] buffer);

        /// <summary>
        /// Resets the <see cref="Position"/> of the current 
        /// <see cref="SoundDataStream"/> to <see cref="TimeSpan.Zero"/>. 
        /// Requires <see cref="CanRewind"/> to be <c>true</c>.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Is thrown when <see cref="CanRewind"/> is <c>false</c>.
        /// </exception>
        public abstract void Rewind();

        /// <summary>
        /// Calculates a <see cref="TimeSpan"/> from a amount of bytes in the
        /// format of the current <see cref="SoundDataStream"/> instance.
        /// </summary>
        /// <param name="byteCount">
        /// The amount of PCM bytes to be used for calculation.
        /// </param>
        /// <returns>
        /// A new <see cref="TimeSpan"/> instance.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="byteCount"/> is less than 0.
        /// </exception>
        public TimeSpan GetTimeFromDecodedByteCount(long byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount));

            return GetTimeFromDecodedByteCount(byteCount, IsStereo,
                SampleRate);
        }

        private static TimeSpan GetTimeFromDecodedByteCount(long byteCount,
            bool isStereo, int sampleRate)
        {
            return TimeSpan.FromSeconds(byteCount / 
                ((double)(isStereo ? ChannelCountStereo : ChannelCountMono)
                * sampleRate));
        }

        /// <summary>
        /// Creates a <see cref="SoundData"/> instance over a Windows-standard 
        /// signed 16-bit PCM WAVE file stream.
        /// </summary>
        /// <param name="stream">
        /// The stream of the WAVE file.
        /// </param>
        /// <param name="disposeStream">
        /// <c>true</c> to dispose the stream when the new 
        /// <see cref="SoundData"/> instance is disposed, 
        /// <c>false</c> otherwise.
        /// </param>
        /// <returns>
        /// A new <see cref="SoundData"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="stream"/> doesn't support reading
        /// and seeking.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when the WAVE format was not supported or malformed.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Is thrown when the end of the stream was reached before the
        /// WAVE header could be read completely.
        /// </exception>
        /// <exception cref="IOException">
        /// Is thrown when an I/O error occurs.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when <see cref="stream"/> was disposed.
        /// </exception>
        public static SoundDataStream OpenWave(Stream stream, 
            bool disposeStream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("The specified stream " +
                    "is not readable.");
            if (!stream.CanSeek)
                throw new ArgumentException("The specified stream " +
                    "is not seekable.");

            bool isStereo;
            int sampleRate, soundDataLengthBytes;

            try
            {
                //Parses the RIFF header first and checks if the format
                //is a normal WAVE file. Other formats are not supported.
                if (stream.ReadStringFixed(4) != WavStream.HeaderStart)
                    throw new Exception("Invalid RIFF file header.");
                if (stream.ReadSignedInteger() !=
                    stream.Length - 8)
                    throw new Exception("The file size doesn't match " +
                        "the size specified in the RIFF header.");
                if (stream.ReadStringFixed(4) != WavStream.HeaderFormat)
                    throw new Exception("Unsupported RIFF format, " +
                        "only WAVE is supported.");

                //Parses the WAVE format definition chunk and extract the
                //sample rate and whether the sound file is stereo or mono.
                string formatChunkStart = stream.ReadStringFixed(4);
                if (formatChunkStart != WavStream.FormatChunkStart &&
                    formatChunkStart != WavStream.FormatChunkStartAlt)
                    throw new Exception("Invalid format chunk header. " +
                        "Expected '" + WavStream.FormatChunkStart + "', " +
                        "got '" + formatChunkStart + "'.");

                int formatChunkSize = stream.ReadSignedInteger();
                if (formatChunkSize != WavStream.FormatChunkSize)
                    throw new Exception("Format chunk size '" 
                        + formatChunkSize + "' not supported, " +
                        "only '16' for PCM is supported.");

                if (stream.ReadShort() != WavStream.FormatCompression)
                    throw new Exception("Unsupported audio format/" +
                        "compression.");
                int channels = stream.ReadShort();
                if (channels == 1) isStereo = false;
                else if (channels == 2) isStereo = true;
                else throw new Exception("Unsupported channel count. " +
                    "Only mono (1) or stereo (2) are supported.");
                sampleRate = stream.ReadSignedInteger();
                if (stream.ReadSignedInteger() != (sampleRate * BitsPerSample
                    * channels) / 8)
                    throw new Exception("Invalid byte rate.");
                if (stream.ReadShort() != (BitsPerSample * channels) / 8)
                    throw new Exception("Invalid block align.");
                if (stream.ReadShort() != 16)
                    throw new Exception("Unsupported sample bit size. " +
                        "Only 16 bit supported.");

                //Parses the first bytes of the data chunk and stores the
                //position in the sound file where the sound data starts.
                if (stream.ReadStringFixed(4) != WavStream.DataChunkStart)
                    throw new Exception("Invalid data chunk header.");

                //If the file contains ID3 tags, the "soundDataLengthBytes"
                //will be slightly smaller than the 
                //"maximumSoundDataLengthBytes" - otherwise, they'll be the
                //same. But the maximum should never be less - if it is, that's
                //probably a sign of data corruption.
                int maximumSoundDataLengthBytes = (int)stream.Length
                    - WavStream.DataOffset;
                soundDataLengthBytes = stream.ReadSignedInteger();
                if (maximumSoundDataLengthBytes < soundDataLengthBytes)
                    throw new Exception("Invalid data chunk size value. " +
                        "Expected maximum of " + maximumSoundDataLengthBytes +
                        ", got " + soundDataLengthBytes + ".");
                if ((int)stream.Position != WavStream.DataOffset)
                    throw new Exception("Stream position doesn't match " +
                        "expected value.");
            }
            catch (Exception exc)
            {
                throw new FormatException("The specified WAV file was " +
                    "invalid.", exc);
            }

            return new WavStream(stream, sampleRate, isStereo,
                soundDataLengthBytes, disposeStream);
        }

        /*
        internal void SaveWAV(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("The specified stream " +
                    "is not writeable.");

            stream.WriteStringFixed(WavDataStream.HeaderStart);
            stream.WriteSignedInteger(WavDataStream.DataOffset + Size - 8);
            stream.WriteStringFixed(WavDataStream.HeaderFormat);

            stream.WriteStringFixed(WavDataStream.FormatChunkStart);
            stream.WriteSignedInteger(WavDataStream.FormatChunkSize);
            stream.WriteShort(WavDataStream.FormatCompression);
            if (IsStereo) stream.WriteShort(2);
            else stream.WriteShort(1);
            stream.WriteSignedInteger(SampleRate);
            stream.WriteSignedInteger((SampleRate * BitsPerSample * Channels)
                / 8);
            stream.WriteShort((short)((BitsPerSample * Channels) / 8));
            stream.WriteShort(BitsPerSample);

            stream.WriteStringFixed(WavDataStream.DataChunkStart);
            stream.WriteSignedInteger(Size);

            int offset = 0, count;
            while (true)
            {
                count = Math.Min(Size - offset, 1024);
                if (count > 0)
                {
                    byte[] samples = GetSamples(offset, count);
                    stream.WriteBuffer(samples, false);
                }
                else break;
            }
        }*/
    }
}
