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
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ShamanTK.Platforms.DesktopGL.Sound
{
    //TODO: Fix bug that playing back a sound right after the loading finalized
    //(or too early in the update method) doesn't work properly.
    /// <summary>
    /// Provides a sound source in three-dimensional space using OpenAL.
    /// </summary>
    class SoundSource : ShamanTK.Sound.SoundSource
    {
        /// <summary>
        /// Defines the length of the first buffer, which is filled with the
        /// available audio data upon initialisation. If the buffer is big
        /// enough to store all the data from the sound stream, the sound 
        /// source is handled like a single sound clip - otherwise, the 
        /// remaining sound data is streamed using buffer sections with the 
        /// size of <see cref="subsequentBufferLength"/>.
        /// </summary>
        private readonly static TimeSpan firstBufferLength =
            TimeSpan.FromSeconds(7);

        /// <summary>
        /// Defines the length of a single buffer for streaming sound content
        /// that is longer than <see cref="firstBufferLength"/>.
        /// </summary>
        private readonly static TimeSpan subsequentBufferLength =
            TimeSpan.FromSeconds(2);

        /// <summary>
        /// Gets a value indicating whether the current 
        /// <see cref="SoundSource"/> is playing (<c>true</c>) or 
        /// paused/stopped (<c>false</c>).
        /// </summary>
        public override bool IsPlaying => !IsDisposed &&
            AL.IsSource(soundSourceHandle) &&
            (AL.GetSourceState(soundSourceHandle) == ALSourceState.Playing);

        /// <summary>
        /// Gets the position of the playback.
        /// </summary>
        public override TimeSpan PlaybackPosition
        {
            get
            {
                float timeSeconds = 0;
                if (!IsDisposed && AL.IsSource(soundSourceHandle))
                    AL.GetSource(soundSourceHandle, ALSourcef.SecOffset,
                        out timeSeconds);

                return TimeSpan.FromSeconds(timeSeconds);
            }
        }

        /// <summary>
        /// Gets or sets the volume of the current <see cref="SoundSource"/>
        /// as a value between 0.0 and 1.0. Values outside that range are
        /// clamped automatically.
        /// </summary>
        public override float Volume
        {
            get => volume;
            set
            {
                volume = Math.Max(Math.Min(1, value), 0);
                AL.Source(soundSourceHandle, ALSourcef.Gain, volume);
            }
        }
        private float volume = 1;

        /// <summary>
        /// Gets a value indicating whether the current 
        /// <see cref="SoundSource"/> instance is asynchronously loading the
        /// sound data into memory (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public override bool IsBuffering => isBuffering;
        private bool isBuffering = false;

        private readonly int soundSourceHandle;
        private readonly int firstBufferSize, subsequentBufferSize;
        protected readonly bool disposeSoundData;

        private const int TargetBufferCount = 2;
        private const int StreamRefreshTimeMs = 100;

        private readonly object internalLock = new object();
        //Access to the following 3 members should only be done with using the
        //lock above!
        private readonly Queue<int> bufferQueue = new Queue<int>();
        private volatile bool startPlaybackFlag = false;
        private volatile bool loopStreamFlag = false;
        private volatile bool preserveBufferQueue = false;
        private readonly SoundDataStream dataStream;

        public SoundSource(SoundDataStream stream, bool disposeSoundData)
            : base(stream.HasLength ? new TimeSpan?(stream.Length) : null)
        {
            dataStream = stream ?? 
                throw new ArgumentNullException(nameof(stream));
            soundSourceHandle = AL.GenSource();
            
            this.disposeSoundData = disposeSoundData;

            firstBufferSize = (int)Math.Ceiling(stream.BytesPerSecond *
                firstBufferLength.TotalSeconds);
            subsequentBufferSize = (int)Math.Ceiling(stream.BytesPerSecond *
                subsequentBufferLength.TotalSeconds);

            new Action(delegate ()
            {
                while (!IsDisposed && !preserveBufferQueue)
                {
                    lock(internalLock) RefreshBufferQueue();
                    Thread.Sleep(StreamRefreshTimeMs);
                }
            }).BeginInvoke(null, null);
        }

        private void RefreshBufferQueue()
        {
            if (IsDisposed || dataStream.IsDisposed || preserveBufferQueue)
                return;

            AL.GetSource(soundSourceHandle, ALGetSourcei.BuffersQueued,
                out int queuedBuffersCount);
            AL.GetSource(soundSourceHandle, ALGetSourcei.BuffersProcessed,
                out int processedBuffersCount);
            ALSourceState sourceState = AL.GetSourceState(soundSourceHandle);

            int unprocessedBuffersCount = 
                queuedBuffersCount - processedBuffersCount;

            //Ensures that the amount of queued sound buffers (parts of the
            //sound stream) is not below the specified treshold. This only 
            //needs to be done when the playback is running or hasn't been 
            //started yet - if it's paused or stopped, this is not required.
            if (queuedBuffersCount == 0 || 
                (unprocessedBuffersCount < TargetBufferCount &&
                (sourceState != ALSourceState.Paused &&
                sourceState != ALSourceState.Stopped) || startPlaybackFlag))
            {
                //The first buffer in the queue has another (larger) size than
                //the other buffers, so that - if the complete sound stream 
                //fits into the first buffer - the sound can be repeatedly 
                //played without streaming it again.
                bool reserveBigBuffer = dataStream.Position == TimeSpan.Zero;
                int newTargetBufferSize = reserveBigBuffer ?
                    firstBufferSize : subsequentBufferSize;

                bool endOfStream = !ContinueBufferSoundDataStream(
                    dataStream, newTargetBufferSize, out int newBufferSize,
                    out int? newBufferHandle);

                if (endOfStream && reserveBigBuffer)
                {
                    //If the whole stream was crammed into the first buffer,
                    //the buffer is kept but the stream is disposed, as it's
                    //no longer needed. The "preserveBufferQueue" flag is set
                    //to true to avoid loosing the buffer during a replay.
                    if (disposeSoundData && !dataStream.IsDisposed)
                        dataStream.Dispose();
                    preserveBufferQueue = true;
                    isBuffering = false;
                }
                else isBuffering = newBufferHandle.HasValue;

                //If newBufferHandle has no value, newBufferSize is 0.
                if (newBufferHandle.HasValue)
                {
                    //There are two seperate queues with a different purpose -
                    //the ALSourceQueue is used for playback and mostly managed
                    //by OpenAL, the "bufferQueue" always contains the most
                    //recent buffers and is used to detect which buffers are
                    //no longer needed and can be disposed.
                    bufferQueue.Enqueue(newBufferHandle.Value);
                    AL.SourceQueueBuffer(soundSourceHandle,
                        newBufferHandle.Value);
                }
            }
            else isBuffering = false;

            //This will only be executed for streaming sound sources.
            if (!preserveBufferQueue)
            {
                AL.GetSource(soundSourceHandle, ALSourceb.Looping,
                    out bool loop);
                if (loop)
                {
                    AL.Source(soundSourceHandle, ALSourceb.Looping,
                        false);
                    loopStreamFlag = true;
                }

                if (sourceState != ALSourceState.Stopped)
                {
                    //If the "bufferQueue" contains more buffers than required
                    //(when the element count is greater than the treshold), 
                    //the "oldest ones" are dequeued and deleted. It's  
                    //important that the queue is not emptied completely here, 
                    //even after the playback has been finished or stopped.
                    //More to that below.
                    while (bufferQueue.Count > TargetBufferCount)
                    {
                        int oldBuffer = bufferQueue.Dequeue();
                        AL.SourceUnqueueBuffers(soundSourceHandle, 1, 
                            ref oldBuffer);
                        AL.DeleteBuffer(oldBuffer);
                    }
                }
                else
                {
                    //Only if the buffer queue contained more than two 
                    //elements - and this is only the case when the sound 
                    //stream didn't fit into the first large buffer - the 
                    //queue should be emptied completely and the stream 
                    //position should be set to the beginning (if possible).
                    if (bufferQueue.Count > 1)
                    {
                        while (bufferQueue.Count > 0)
                        {
                            int oldBuffer = bufferQueue.Dequeue();
                            AL.SourceUnqueueBuffers(soundSourceHandle, 1,
                                ref oldBuffer);
                            AL.DeleteBuffer(oldBuffer);
                        }
                        if (dataStream.CanRewind) 
                            dataStream.Rewind();
                        if (loopStreamFlag)
                        {
                            loopStreamFlag = false;
                            startPlaybackFlag = true;
                            AL.Source(soundSourceHandle, ALSourceb.Looping, 
                                true);
                        }
                    }
                }
            }

            AL.GetSource(soundSourceHandle, ALGetSourcei.BuffersQueued,
                out int remainingQueuedBuffersCount);
            if (startPlaybackFlag && remainingQueuedBuffersCount > 0)
            {
                AL.SourcePlay(soundSourceHandle);   
                startPlaybackFlag = false;
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
        public override void Play(bool loop)
        {
            ThrowIfDisposed();

            AL.Source(soundSourceHandle, ALSourceb.Looping, loop);

            if (IsBuffering) startPlaybackFlag = true;
            else AL.SourcePlay(soundSourceHandle);
        }

        /// <summary>
        /// Pauses the playback at the current position.
        /// This method has no effect when <see cref="IsPlaying"/> is
        /// <c>false</c>.
        /// </summary>
        /// <returns>
        /// <c>true</c> when the playback was paused and can be continued at
        /// the same position as it was interrupted,
        /// <c>false</c> if resuming at that position isn't possible and
        /// continuing the playback with <see cref="Play"/> will cause the
        /// playback to continue at the now current position of the associated
        /// <see cref="IO.SoundDataStream"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current <see cref="SoundSource"/> instance
        /// was disposed and can't be used anymore.
        /// </exception>
        public override void Pause()
        {
            ThrowIfDisposed();
            AL.SourcePause(soundSourceHandle);
        }

        /// <summary>
        /// Stops the playback and sets the playback position to 0.
        /// If <see cref="IsPlaying"/> is <c>false</c>, only the playback 
        /// position is reset to 0.
        /// </summary>
        /// <returns>
        /// <c>true</c> when the playback was stopped and can be continued at
        /// the beginning again,
        /// <c>false</c> if resuming at the beginning isn't possible and
        /// continuing the playback with <see cref="Play"/> will cause the
        /// playback to continue at the now current position of the associated
        /// <see cref="IO.SoundDataStream"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when the current <see cref="SoundSource"/> instance
        /// was disposed and can't be used anymore.
        /// </exception>
        public override void Stop()
        {
            ThrowIfDisposed();
            AL.SourceStop(soundSourceHandle);
        }

        protected override void Dispose(bool disposing)
        {
            lock (internalLock)
            {
                if (disposing && dataStream != null && disposeSoundData)
                    dataStream.Dispose();

                if (AL.IsSource(soundSourceHandle))
                    AL.DeleteSource(soundSourceHandle);

                while (bufferQueue.Count > 0)
                {
                    int bufferHandle = bufferQueue.Dequeue();
                    if (AL.IsBuffer(bufferHandle))
                        AL.DeleteBuffer(bufferHandle);
                }
            }
        }

        private static TimeSpan GetPlaybackPosition(int sourceHandle)
        {
            float timeSeconds = 0;

            if (AL.IsSource(sourceHandle))
                AL.GetSource(sourceHandle, ALSourcef.SecOffset,
                    out timeSeconds);

            return TimeSpan.FromSeconds(timeSeconds);
        }

        private static bool ContinueBufferSoundDataStream(
            SoundDataStream dataStream, int size, out int actualBufferSize,
            out int? bufferHandle)
        {
            if (dataStream == null)
                throw new ArgumentNullException(nameof(dataStream));
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

            byte[] streamBuffer = new byte[size];
            actualBufferSize = dataStream.ReadSamples(streamBuffer);

            if (actualBufferSize > 0)
            {
                bufferHandle = AL.GenBuffer();
                ALFormat bufferFormat = dataStream.IsStereo ?
                    ALFormat.Stereo16 : ALFormat.Mono16;

                AL.BufferData(bufferHandle.Value, bufferFormat, streamBuffer,
                    dataStream.SampleRate);
            }
            else bufferHandle = null;

            //If the amount of decoded bytes doesn't match (is less than) the
            //amount of "expected" or requested bytes, the end of the stream
            //is reached and further buffers don't have to be created.
            return size == actualBufferSize;
        }
    }
}
