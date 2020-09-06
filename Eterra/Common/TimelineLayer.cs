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
    /// Provides a collection of <see cref="Keyframe"/> instances, 
    /// hierarchically structured into contained 
    /// <see cref="TimelineChannel"/> instances per
    /// object parameter.
    /// </summary>
    public class TimelineLayer : IEnumerable<TimelineChannel>
    {
        private readonly Dictionary<string, TimelineChannel> channels =
            new Dictionary<string, TimelineChannel>();

        /// <summary>
        /// Gets the distance between the first and the last keyframe.
        /// </summary>
        public TimeSpan Length { get; }

        /// <summary>
        /// Gets the position of the first keyframe.
        /// </summary>
        public TimeSpan Start { get; }

        /// <summary>
        /// Gets the position of the last keyframe.
        /// </summary>
        public TimeSpan End { get; }

        /// <summary>
        /// Gets the identifier of the current instance.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Gets the amount of channels.
        /// </summary>
        public int ChannelCount => channels.Count;

        /// <summary>
        /// Gets a value indicating whether the current instance contains at 
        /// least one <see cref="TimelineChannel"/> that contains at least one 
        /// <see cref="Keyframe"/> (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool HasKeyframes { get; }

        /// <summary>
        /// Gets a value indicating whether the current instance contains at 
        /// least one <see cref="TimelineChannel"/> (<c>true</c>) or not 
        /// (<c>false</c>).
        /// </summary>
        public bool HasChannels { get; }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="TimelineLayer"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the new <see cref="TimelineLayer"/> instance.
        /// </param>
        /// <param name="channels">
        /// An enumeration of <see cref="TimelineChannel"/> instances,
        /// which will be held by the new <see cref="TimelineLayer"/>
        /// instance.
        /// </param>
        public TimelineLayer(string identifier, 
            IEnumerable<TimelineChannel> channels)
        {
            Identifier = identifier ??
                throw new ArgumentNullException(nameof(identifier));

            foreach (TimelineChannel channel in channels)
            {
                if (channel == null) continue;
                if (this.channels.ContainsKey(channel.Identifier.Identifier))
                    throw new ArgumentException("The enumeration of " +
                        "timeline channels contains at least two instances " +
                        "with the same identifier.");
                else this.channels[channel.Identifier.Identifier] = channel;
            }

            TimeSpan start = TimeSpan.MaxValue;
            TimeSpan end = TimeSpan.MinValue;

            HasKeyframes = false;
            HasChannels = false;

            foreach (TimelineChannel channel in channels)
            {
                HasChannels = true;
                HasKeyframes |= channel.HasKeyframes;

                if (channel.Start < start) start = channel.Start;
                if (channel.End > end) end = channel.End;                
            }

            //The position of the first marker or keyframe.
            Start = start != TimeSpan.MaxValue ? start : TimeSpan.Zero;
            //The position of the last marker or keyframe.
            End = end != TimeSpan.MinValue ? end : TimeSpan.Zero;
            //The distance between the start and the end alias timeline length.
            Length = End - Start;
        }

        /// <summary>
        /// Gets a <see cref="TimelineChannel{T}"/> from the current instance.
        /// </summary>
        /// <typeparam name="T">
        /// The value type of the keyframes in the 
        /// <see cref="TimelineChannel{T}"/>.
        /// </typeparam>
        /// <param name="channelIdentifier">
        /// The channel identifier of the <see cref="TimelineChannel{T}"/>
        /// to be returned.
        /// </param>
        /// <returns>
        /// The requested <see cref="TimelineChannel{T}"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="channelIdentifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="TimelineChannel{T}"/> with the
        /// specified <paramref name="channelIdentifier"/> was found, when
        /// the type constraint in the specfieid 
        /// <paramref name="channelIdentifier"/> doesn't match with the 
        /// <see cref="TimelineChannel.ValueType"/> of the 
        /// <see cref="TimelineChannel{T}"/> or when
        /// the type specified through <typeparamref name="T"/> doesn't match
        /// with the type of the <see cref="TimelineChannel{T}"/>.
        /// </exception>
        public TimelineChannel<T> GetChannel<T>(
            ChannelIdentifier channelIdentifier)
            where T : unmanaged
        {
            if (channelIdentifier == null)
                throw new ArgumentNullException(nameof(channelIdentifier));

            if (channels.TryGetValue(channelIdentifier.Identifier,
                out TimelineChannel channel))
            {
                if (channelIdentifier.ValueTypeConstraint.IsAssignableFrom(
                    channel.Identifier.ValueTypeConstraint))
                {
                    if (channel is TimelineChannel<T> typedChannel)
                        return typedChannel;
                    else throw new ArgumentException("A channel with the " +
                        "specified identifier was found, but couldn't be " +
                        "converted to the specified type.");
                }
                else throw new ArgumentException("A channel with the " +
                    "specified identifier name was found, but with an " +
                    "incompatible type.");
            }
            else throw new ArgumentException("A channel with the " +
              "specified identifier name couldn't be found.");
        }

        /// <summary>
        /// Gets a <see cref="TimelineChannel"/> from the current instance.
        /// </summary>
        /// <param name="channelIdentifier">
        /// The channel identifier of the <see cref="TimelineChannel"/>
        /// to be returned.
        /// </param>
        /// <returns>
        /// The requested <see cref="TimelineChannel"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="channelIdentifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="TimelineChannel"/> with the
        /// specified <paramref name="channelIdentifier"/> was found or when
        /// the type constraint in the specfieid 
        /// <paramref name="channelIdentifier"/> doesn't match with the 
        /// <see cref="TimelineChannel.ValueType"/> of the 
        /// <see cref="TimelineChannel"/>.
        /// </exception>
        public TimelineChannel GetChannel(ChannelIdentifier channelIdentifier)
        {
            if (channelIdentifier == null)
                throw new ArgumentNullException(nameof(channelIdentifier));

            if (channels.TryGetValue(channelIdentifier.Identifier,
                out TimelineChannel channel))
            {
                if (channelIdentifier.ValueTypeConstraint.IsAssignableFrom(
                    channel.ValueType))
                {
                    return channel;
                }
                else throw new ArgumentException("A channel with the " +
                    "specified identifier name was found, but with an " +
                    "incompatible type.");
            }
            else throw new ArgumentException("A channel with the " +
              "specified identifier name couldn't be found.");
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator{T}"/> instance.</returns>
        public IEnumerator<TimelineChannel> GetEnumerator()
        {
            return channels.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator"/> instance.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return channels.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"\"{Identifier}\" (Channels: {ChannelCount}, " +
                $"Length: {Length})";
        }
    }
}
