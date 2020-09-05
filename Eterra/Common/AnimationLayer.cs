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
    /// Represents a collection of <see cref="AnimationChannel"/> instances,
    /// where each represents an animation of a single object parameter.
    /// </summary>
    public class AnimationLayer : IEnumerable<AnimationChannel>
    {
        /// <summary>
        /// Gets the identifier of the layer, which is used by the current
        /// <see cref="AnimationLayer"/>.
        /// </summary>
        public string Identifier { get; }

        private Dictionary<string, AnimationChannel> channels =
            new Dictionary<string, AnimationChannel>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimationLayer"/> 
        /// class.
        /// </summary>
        /// <param name="parentAnimation">
        /// The <see cref="Animation"/> instance this channel belongs to
        /// and takes its playback position updates from.
        /// </param>
        /// <param name="sourceLayer">
        /// The <see cref="TimelineLayer"/> instance the new
        /// <see cref="AnimationChannel"/> instance will take its channels
        /// (and the associated keyframe data) from.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="parentAnimation"/> or
        /// <paramref name="sourceLayer"/> are null.
        /// </exception>
        internal AnimationLayer(Animation parentAnimation, 
            TimelineLayer sourceLayer)
        {
            if (sourceLayer == null)
                throw new ArgumentNullException(nameof(sourceLayer));
            Identifier = sourceLayer.Identifier;

            foreach (TimelineChannel sourceChannel in sourceLayer)
                channels[sourceChannel.Identifier.Identifier] =
                    AnimationChannel.Create(parentAnimation, sourceChannel);
        }

        /// <summary>
        /// Attempts to get an <see cref="AnimationChannel"/> from the current 
        /// <see cref="AnimationLayer"/> instance.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the <see cref="AnimationChannel"/>.
        /// </param>
        /// <param name="animationChannel">
        /// The requested <see cref="AnimationChannel"/> or null.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="identifier"/> could
        /// be resolved into an existing <see cref="AnimationChannel"/> which
        /// matches the <see cref="ChannelIdentifier.ValueTypeConstraint"/>,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        public bool TryGetChannel(ChannelIdentifier identifier,
            out AnimationChannel animationChannel)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (channels.TryGetValue(identifier.Identifier,
                out AnimationChannel animationChannelUntyped))
            {
                if (animationChannelUntyped.Identifier.MatchesConstraint(
                        identifier))
                {
                    animationChannel = animationChannelUntyped;
                    return true;
                }
            }

            animationChannel = null;
            return false;
        }

        /// <summary>
        /// Attempts to get an <see cref="AnimationChannel{T}"/> from the 
        /// current <see cref="AnimationLayer"/> instance.
        /// </summary>
        /// <typeparam name="T">
        /// The value type of the <see cref="AnimationChannel{T}"/> to be
        /// retrieved.
        /// </typeparam>
        /// <param name="identifier">
        /// The identifier of the <see cref="AnimationChannel{T}"/>.
        /// </param>
        /// <param name="animationChannel">
        /// The requested <see cref="AnimationChannel{T}"/> or null.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="identifier"/> could be
        /// resolved into an existing <see cref="AnimationChannel{T}"/> which
        /// matches the <see cref="ChannelIdentifier.ValueTypeConstraint"/> and
        /// has the value type specified in <typeparamref name="T"/>,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        public bool TryGetChannel<T>(ChannelIdentifier identifier,
            out AnimationChannel<T> animationChannel)
            where T : unmanaged
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (channels.TryGetValue(identifier.Identifier,
                out AnimationChannel animationChannelUntyped))
            {
                if (animationChannelUntyped is
                    AnimationChannel<T> animationChannelTyped &&
                    animationChannelUntyped.Identifier.MatchesConstraint(
                        identifier))
                {
                    animationChannel = animationChannelTyped;
                    return true;
                }
            }

            animationChannel = null;
            return false;
        }

        /// <summary>
        /// Gets a <see cref="AnimationChannel"/> from the current
        /// <see cref="AnimationLayer"/> instance.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the <see cref="AnimationChannel"/>.
        /// </param>
        /// <returns>
        /// The requested <see cref="AnimationChannel"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="AnimationChannel"/> instance that
        /// matches with the <see cref="ChannelIdentifier.Identifier"/> and the
        /// <see cref="ChannelIdentifier.ValueTypeConstraint"/> of the 
        /// specified <paramref name="identifier"/> was found.
        /// </exception>
        public AnimationChannel GetChannel(ChannelIdentifier identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (channels.TryGetValue(identifier.Identifier,
                out AnimationChannel animationChannelUntyped))
            {
                if (animationChannelUntyped.Identifier.MatchesConstraint(
                        identifier))
                {
                    return animationChannelUntyped;
                }
                else throw new ArgumentException("A channel with the " +
                    "specified identifier was found, but didn't match " +
                    "the identifiers type constraint.");
            }
            else throw new ArgumentException("A channel with the specified " +
                "identifier couldn't be found.");
        }

        /// <summary>
        /// Gets a <see cref="AnimationChannel{T}"/> from the current
        /// <see cref="AnimationLayer"/> instance.
        /// </summary>
        /// <typeparam name="T">
        /// The value type of the requested <see cref="AnimationChannel{T}"/>.
        /// </typeparam>
        /// <param name="identifier">
        /// The identifier of the <see cref="AnimationChannel{T}"/>.
        /// </param>
        /// <returns>
        /// The requested <see cref="AnimationChannel{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="AnimationChannel{T}"/> instance that
        /// matches with the <see cref="ChannelIdentifier.Identifier"/> and the
        /// <see cref="ChannelIdentifier.ValueTypeConstraint"/> of the 
        /// specified <paramref name="identifier"/> was found or when the
        /// value type of the requested <see cref="AnimationChannel{T}"/> 
        /// doesn't match with the type specified in <typeparamref name="T"/>.
        /// </exception>
        public AnimationChannel<T> GetChannel<T>(ChannelIdentifier identifier)
            where T : unmanaged
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (channels.TryGetValue(identifier.Identifier,
                out AnimationChannel animationChannelUntyped))
            {
                if (animationChannelUntyped is
                    AnimationChannel<T> animationChannelTyped &&
                    animationChannelUntyped.Identifier.MatchesConstraint(
                        identifier))
                {
                    return animationChannelTyped;
                }
                else throw new ArgumentException("A channel with the " +
                    "specified identifier was found, but had a different " +
                    "type then what was requested or didn't match the " +
                    "identifier type constraint.");
            }
            else throw new ArgumentException("A channel with the specified " +
                "identifier couldn't be found.");
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator{T}"/> instance.</returns>
        public IEnumerator<AnimationChannel> GetEnumerator()
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
    }
}
