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
using System.Collections;
using System.Collections.Generic;

namespace Eterra.Common
{
    /// <summary>
    /// Represents a base class from which classes are derived that 
    /// provides a collection of <see cref="Keyframe"/> instances for a 
    /// specific object parameter.
    /// </summary>
    public abstract class TimelineChannel : IEnumerable<Keyframe>
    {
        /// <summary>
        /// Gets the amount of keyframes.
        /// </summary>
        public abstract int KeyframeCount { get; }

        /// <summary>
        /// Gets the position of the first keyframe.
        /// </summary>
        public abstract TimeSpan Start { get; }

        /// <summary>
        /// Gets the position of the last keyframe.
        /// </summary>
        public abstract TimeSpan End { get; }

        /// <summary>
        /// Gets the distance between the first and the last keyframe.
        /// </summary>
        public abstract TimeSpan Length { get; }

        /// <summary>
        /// Gets the type of the keyframe values.
        /// </summary>
        public abstract Type ValueType { get; }

        /// <summary>
        /// Gets the identifier of the current instance.
        /// </summary>
        public abstract ChannelIdentifier Identifier { get; }

        /// <summary>
        /// Gets the interpolation method, with which the values between the
        /// individual keyframes should be calculated.
        /// </summary>
        public abstract InterpolationMethod InterpolationMethod { get; }

        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator{T}"/> instance.</returns>
        public abstract IEnumerator<Keyframe> GetEnumerator();

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator"/> instance.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Provides a collection of <see cref="Keyframe"/> instances for a 
    /// single object parameter.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the object parameter values.
    /// </typeparam>
    public class TimelineChannel<T> : TimelineChannel, 
        IEnumerable<Keyframe<T>> where T : unmanaged
    {
        #region Internally used IEnumerator implementation for TimelineLayer
        private class KeyframeEnumerator : IEnumerator<Keyframe<T>>
        {
            public Keyframe<T> Current
            {
                get
                {
                    if (currentKeyframeIndex >= 0
                        && currentKeyframeIndex < channel.KeyframeCount)
                        return channel.GetKeyframe(currentKeyframeIndex);
                    else return Keyframe<T>.Empty;
                }
            }

            object IEnumerator.Current => Current;

            private readonly TimelineChannel<T> channel;

            private int currentKeyframeIndex = 0;

            public KeyframeEnumerator(TimelineChannel<T> channel)
            {
                this.channel = channel ??
                    throw new ArgumentNullException(nameof(channel));
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (currentKeyframeIndex + 1 < channel.KeyframeCount)
                {
                    currentKeyframeIndex++;
                    return true;
                }
                else return false;
            }

            public void Reset()
            {
                currentKeyframeIndex = 0;
            }
        }
        #endregion

        private readonly SortedList<TimeSpan, Keyframe<T>> data =
            new SortedList<TimeSpan, Keyframe<T>>();

        /// <summary>
        /// Gets the distance between the first and the last keyframe.
        /// </summary>
        public override TimeSpan Length { get; }

        /// <summary>
        /// Gets the position of the first keyframe.
        /// </summary>
        public override TimeSpan Start { get; }

        /// <summary>
        /// Gets the position of the last keyframe.
        /// </summary>
        public override TimeSpan End { get; }

        /// <summary>
        /// Gets the amount of keyframes.
        /// </summary>
        public override int KeyframeCount => data.Count;

        /// <summary>
        /// Gets the type of the keyframe values.
        /// </summary>
        public override Type ValueType => typeof(T);

        /// <summary>
        /// Gets the interpolation method, with which the values between the
        /// individual keyframes should be calculated.
        /// </summary>
        public override InterpolationMethod InterpolationMethod { get; }

        /// <summary>
        /// Gets the identifier of the current instance, which needs to be 
        /// unique within any parent <see cref="TimelineLayer"/>.
        /// </summary>
        public override ChannelIdentifier Identifier { get; }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="TimelineChannel{T}"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the new channel instance.
        /// </param>
        /// <param name="keyframes">
        /// An enumerable collection of <see cref="Keyframe{T}"/> instances,
        /// which make up the new channel instance.
        /// </param>
        /// <param name="interpolationMethod">
        /// The requested interpolation method, with which the values between 
        /// the individual keyframes should be calculated.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> or 
        /// <paramref name="keyframes"/> are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <see cref="ChannelIdentifier.ValueTypeConstraint"/>
        /// doesn't match with the type specified by <typeparamref name="T"/>,
        /// when <paramref name="interpolationMethod"/> is 
        /// invalid or when <paramref name="keyframes"/> contains at least
        /// two <see cref="Keyframe"/> instances with the same
        /// <see cref="Keyframe.Position"/>.
        /// </exception>
        public TimelineChannel(ChannelIdentifier identifier,
            InterpolationMethod interpolationMethod,
            IEnumerable<Keyframe<T>> keyframes)
        {
            Identifier = identifier ??
                throw new ArgumentNullException(nameof(identifier));
            if (!identifier.ValueTypeConstraint.IsAssignableFrom(typeof(T)))
                throw new ArgumentException("The specified channel " +
                    "identifier can not be used on a channel with the " +
                    $"type {typeof(T).Name}.");

            if (!Enum.IsDefined(typeof(InterpolationMethod),
                interpolationMethod))
                throw new ArgumentException("The specified interpolation " +
                    "mode is invalid.");

            InterpolationMethod = interpolationMethod;

            foreach (Keyframe<T> keyframe in keyframes)
            {
                if (keyframe == null) continue;
                if (data.ContainsKey(keyframe.Position))
                    throw new ArgumentException("The enumeration contains " +
                        "at least two keyframes with the same position.");
                else data[keyframe.Position] = keyframe;
            }

            Start = data.Count > 0 ? data.Keys[0] : TimeSpan.Zero;
            End = data.Count > 0 ? data.Keys[data.Count - 1] : TimeSpan.Zero;
            Length = End - Start;
        }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="TimelineLayer{T}"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the new channel instance.
        /// </param>
        /// <param name="keyframes">
        /// An enumerable collection of <see cref="Keyframe"/> instances,
        /// which are casted to <see cref="Keyframe{T}"/> of 
        /// <typeparamref name="T"/> in the constructor.
        /// </param>
        /// <param name="interpolationMethod">
        /// The requested interpolation method, with which the values between 
        /// the individual keyframes should be calculated.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> or 
        /// <paramref name="keyframes"/> are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the <see cref="ChannelIdentifier.ValueTypeConstraint"/>
        /// doesn't match with the type specified by <typeparamref name="T"/>,
        /// when <paramref name="interpolationMethod"/> is 
        /// invalid, when <paramref name="keyframes"/> contains at least
        /// two <see cref="Keyframe"/> instances with the same
        /// <see cref="Keyframe.Position"/> or when one of the
        /// <see cref="Keyframe"/> instances in <paramref name="keyframes"/>
        /// contains an instance which is not of type
        /// <see cref="Keyframe{T}"/> with <c>T</c> being 
        /// <typeparamref name="T"/>.
        /// </exception>
        public TimelineChannel(ChannelIdentifier identifier,
            InterpolationMethod interpolationMethod,
            IEnumerable<Keyframe> keyframes)
        {
            Identifier = identifier ??
                throw new ArgumentNullException(nameof(identifier));
            if (!identifier.ValueTypeConstraint.IsAssignableFrom(typeof(T)))
                throw new ArgumentException("The specified channel " +
                    "identifier can not be used on a channel with the " +
                    $"type {typeof(T).Name}.");

            if (!Enum.IsDefined(typeof(InterpolationMethod),
                interpolationMethod))
                throw new ArgumentException("The specified interpolation " +
                    "mode is invalid.");

            InterpolationMethod = interpolationMethod;

            foreach (Keyframe keyframe in keyframes)
            {
                if (keyframe == null) continue;
                if (data.ContainsKey(keyframe.Position))
                    throw new ArgumentException("The enumeration contains " +
                        "at least two keyframes with the same position.");
                else try { data[keyframe.Position] = (Keyframe<T>)keyframe; }
                    catch (Exception exc)
                    {
                        throw new ArgumentException("One of the keyframe " +
                            "instances in the enumeration has an invalid " +
                            "type.", exc);
                    }
            }

            Start = data.Count > 0 ? data.Keys[0] : TimeSpan.Zero;
            End = data.Count > 0 ? data.Keys[data.Count - 1] : TimeSpan.Zero;
            Length = End - Start;
        }

        /// <summary>
        /// Gets a keyframe by its index.
        /// </summary>
        /// <param name="index">
        /// The index of the <see cref="Keyframe{T}"/> to be retrieved.
        /// </param>
        /// <returns>
        /// The requested <see cref="Keyframe{T}"/> instance.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="index"/> is less than 0 or
        /// greater than/equal to <see cref="KeyframeCount"/>.
        /// </exception>
        public Keyframe<T> GetKeyframe(int index)
        {
            if (index >= 0 && index < KeyframeCount)
                return data.Values[index];
            else throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <summary>
        /// Finds the closest keyframe after or equal to a timeline position.
        /// </summary>
        /// <param name="position">
        /// The timeline position.
        /// </param>
        /// <param name="keyframeOffset">
        /// The offset (in keyframes), if a keyframe before (negative value) 
        /// or after (positive value) the keyframe nearest to the 
        /// <paramref name="position"/> should be returned instead, or 0 to 
        /// retrieve the closest keyframe.
        /// </param>
        /// <param name="frame">
        /// The closest keyframe after to the specified
        /// <paramref name="position"/> or <see cref="Keyframe{T}.Empty"/>, if
        /// no keyframe with the specified criteria was found.
        /// </param>
        /// <returns>
        /// <c>true</c> if the closest keyframe before <see cref="position"/>
        /// with the specified <paramref name="keyframeOffset"/> was found,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <remarks>
        /// If no keyframe after the specified <paramref name="position"/>
        /// was found, the method will return false even if the value
        /// <paramref name="keyframeOffset"/> is less than 0.
        /// </remarks>
        public bool TryFindKeyframeAfter(TimeSpan position, int keyframeOffset,
            out Keyframe<T> frame)
        {
            int index = GetNearestKeyframeIndex(position);
            if (data.Keys[index] <= position) index += keyframeOffset + 1;
            else index += keyframeOffset;

            if (index >= 0 && index < KeyframeCount)
            {
                frame = data.Values[index];
                return true;
            }

            frame = Keyframe<T>.Empty;
            return false;
        }

        /// <summary>
        /// Finds the closest keyframe before or equal to a timeline position.
        /// </summary>
        /// <param name="position">
        /// The timeline position.
        /// </param>
        /// <param name="keyframeOffset">
        /// The offset (in keyframes), if a keyframe before (negative value) 
        /// or after (positive value) the keyframe nearest to the 
        /// <paramref name="position"/> should be returned instead, or 0 to 
        /// retrieve the closest keyframe.
        /// </param>
        /// <param name="frame">
        /// The closest keyframe before to the specified
        /// <paramref name="position"/> or <see cref="Keyframe{T}.Empty"/>, if
        /// no keyframe with the specified criteria was found.
        /// </param>
        /// <returns>
        /// <c>true</c> if the closest keyframe before <see cref="position"/>
        /// with the specified <paramref name="keyframeOffset"/> was found,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <remarks>
        /// If no keyframe before the specified <paramref name="position"/>
        /// was found, the method will return false even if the value
        /// <paramref name="keyframeOffset"/> is greater than 0.
        /// </remarks>
        public bool TryFindKeyframeBefore(TimeSpan position,
            int keyframeOffset, out Keyframe<T> frame)
        {
            int index = GetNearestKeyframeIndex(position) + keyframeOffset;

            if (index >= 0 && index < KeyframeCount)
            {
                if (data.Keys[index] <= position)
                {
                    frame = data.Values[index];
                    return true;
                }
            }

            frame = Keyframe<T>.Empty;
            return false;
        }

        /// <summary>
        /// Finds the closest keyframe to a timeline position.
        /// </summary>
        /// <param name="position">
        /// The timeline position.
        /// </param>
        /// <param name="keyframeOffset">
        /// The offset (in keyframes), if a keyframe before or after the
        /// keyframe nearest to the <paramref name="position"/> should be
        /// returned instead, or 0 to retrieve the closest keyframe.
        /// </param>
        /// <param name="frame">
        /// The keyframe closest to the specified <paramref name="position"/>
        /// or <see cref="Keyframe{T}.Empty"/>, if no keyframe with the
        /// specified criteria was found.
        /// </param>
        /// <returns>
        /// <c>true</c> if a keyframe near <paramref name="position"/> with the
        /// specified <paramref name="keyframeOffset"/> was found,
        /// <c>false</c> otherwise.
        /// </returns>
        public bool TryFindKeyframe(TimeSpan position, int keyframeOffset,
            out Keyframe<T> frame)
        {
            int count = KeyframeCount;
            int index = GetNearestKeyframeIndex(position);

            //Ensure that the keyframe with the previously retrieved index is
            //actually the closest one to the requested time position
            if (index >= 0 && (index + 1) < count) //Covers count == 0
            {
                TimeSpan left = data.Keys[index];
                TimeSpan right = data.Keys[index + 1];
                if (position - left < right - position) index += 1;
            }

            int indexOffset = index + keyframeOffset;
            if (count > 0 && indexOffset < count && indexOffset >= 0)
            {
                frame = data.Values[indexOffset];
                return true;
            }
            else
            {
                frame = Keyframe<T>.Empty;
                return false;
            }
        }

        /// <summary>
        /// Get the index of the nearest keyframe before - or, if there are no
        /// keyframes before the specified position - after a
        /// timeline position.
        /// </summary>
        /// <param name="position">
        /// The position to be used to search the keyframe.
        /// </param>
        /// <returns>The index of the keyframe.</returns>
        private int GetNearestKeyframeIndex(TimeSpan position)
        {
            int lower = 0;
            int upper = KeyframeCount - 1;
            while (lower <= upper)
            {
                int middle = lower + (upper - lower) / 2;
                int compareResult = position.CompareTo(data.Keys[middle]);
                if (compareResult == 0) return middle;
                else if (compareResult < 0) upper = middle - 1;
                else lower = middle + 1;
            }

            return Math.Max(Math.Min(lower, upper), 0);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator{T}"/> instance.</returns>
        IEnumerator<Keyframe<T>> IEnumerable<Keyframe<T>>.GetEnumerator()
        {
            return new KeyframeEnumerator(this);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator{T}"/> instance.</returns>
        public override IEnumerator<Keyframe> GetEnumerator()
        {
            return new KeyframeEnumerator(this);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator"/> instance.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new KeyframeEnumerator(this);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"{Identifier} ({ValueType.Name}), {Length}";
        }
    }
}
