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

using System;
using System.Collections;
using System.Collections.Generic;

namespace ShamanTK.Common
{
    /// <summary>
    /// Represents a collection of <see cref="AnimationParameter"/> instances,
    /// where each represents an animation of a single object parameter.
    /// </summary>
    public class AnimationLayer : IEnumerable<AnimationParameter>
    {
        /// <summary>
        /// Gets the identifier of the layer, which is used by the current
        /// <see cref="AnimationLayer"/>.
        /// </summary>
        public string Identifier { get; }

        private Dictionary<string, AnimationParameter> parameters =
            new Dictionary<string, AnimationParameter>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimationLayer"/> 
        /// class.
        /// </summary>
        /// <param name="parentAnimation">
        /// The <see cref="Animation"/> instance this parameter belongs to
        /// and takes its playback position updates from.
        /// </param>
        /// <param name="sourceLayer">
        /// The <see cref="TimelineLayer"/> instance the new
        /// <see cref="AnimationParameter"/> instance will take its parameters
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

            foreach (TimelineParameter sourceParameter in sourceLayer)
                parameters[sourceParameter.Identifier.Name] =
                    AnimationParameter.Create(parentAnimation, sourceParameter);
        }

        /// <summary>
        /// Attempts to get an <see cref="AnimationParameter"/> from the current 
        /// <see cref="AnimationLayer"/> instance.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the <see cref="AnimationParameter"/>.
        /// </param>
        /// <param name="animationParameter">
        /// The requested <see cref="AnimationParameter"/> or null.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="identifier"/> could
        /// be resolved into an existing <see cref="AnimationParameter"/> which
        /// matches the <see cref="ParameterIdentifier.ValueTypeConstraint"/>,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        public bool TryGetParameter(ParameterIdentifier identifier,
            out AnimationParameter animationParameter)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (parameters.TryGetValue(identifier.Name,
                out AnimationParameter animationParameterUntyped))
            {
                if (animationParameterUntyped.Identifier.MatchesConstraint(
                        identifier))
                {
                    animationParameter = animationParameterUntyped;
                    return true;
                }
            }

            animationParameter = null;
            return false;
        }

        /// <summary>
        /// Attempts to get an <see cref="AnimationParameter{T}"/> from the 
        /// current <see cref="AnimationLayer"/> instance.
        /// </summary>
        /// <typeparam name="T">
        /// The value type of the <see cref="AnimationParameter{T}"/> to be
        /// retrieved.
        /// </typeparam>
        /// <param name="identifier">
        /// The identifier of the <see cref="AnimationParameter{T}"/>.
        /// </param>
        /// <param name="animationParameter">
        /// The requested <see cref="AnimationParameter{T}"/> or null.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="identifier"/> could be
        /// resolved into an existing <see cref="AnimationParameter{T}"/> which
        /// matches the <see cref="ParameterIdentifier.ValueTypeConstraint"/> and
        /// has the value type specified in <typeparamref name="T"/>,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        public bool TryGetParameter<T>(ParameterIdentifier identifier,
            out AnimationParameter<T> animationParameter)
            where T : unmanaged
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (parameters.TryGetValue(identifier.Name,
                out AnimationParameter animationParameterUntyped))
            {
                if (animationParameterUntyped is
                    AnimationParameter<T> animationParameterTyped &&
                    animationParameterUntyped.Identifier.MatchesConstraint(
                        identifier))
                {
                    animationParameter = animationParameterTyped;
                    return true;
                }
            }

            animationParameter = null;
            return false;
        }

        /// <summary>
        /// Gets a <see cref="AnimationParameter"/> from the current
        /// <see cref="AnimationLayer"/> instance.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the <see cref="AnimationParameter"/>.
        /// </param>
        /// <returns>
        /// The requested <see cref="AnimationParameter"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="AnimationParameter"/> instance that
        /// matches with the <see cref="ParameterIdentifier.Name"/> and the
        /// <see cref="ParameterIdentifier.ValueTypeConstraint"/> of the 
        /// specified <paramref name="identifier"/> was found.
        /// </exception>
        public AnimationParameter GetParameter(ParameterIdentifier identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (parameters.TryGetValue(identifier.Name,
                out AnimationParameter animationParameterUntyped))
            {
                if (animationParameterUntyped.Identifier.MatchesConstraint(
                        identifier))
                {
                    return animationParameterUntyped;
                }
                else throw new ArgumentException("A parameter with the " +
                    "specified identifier was found, but didn't match " +
                    "the identifiers type constraint.");
            }
            else throw new ArgumentException("A parameter with the specified " +
                "identifier couldn't be found.");
        }

        /// <summary>
        /// Gets a <see cref="AnimationParameter{T}"/> from the current
        /// <see cref="AnimationLayer"/> instance.
        /// </summary>
        /// <typeparam name="T">
        /// The value type of the requested <see cref="AnimationParameter{T}"/>.
        /// </typeparam>
        /// <param name="identifier">
        /// The identifier of the <see cref="AnimationParameter{T}"/>.
        /// </param>
        /// <returns>
        /// The requested <see cref="AnimationParameter{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="AnimationParameter{T}"/> instance that
        /// matches with the <see cref="ParameterIdentifier.Name"/> and the
        /// <see cref="ParameterIdentifier.ValueTypeConstraint"/> of the 
        /// specified <paramref name="identifier"/> was found or when the
        /// value type of the requested <see cref="AnimationParameter{T}"/> 
        /// doesn't match with the type specified in <typeparamref name="T"/>.
        /// </exception>
        public AnimationParameter<T> GetParameter<T>(
            ParameterIdentifier identifier)
            where T : unmanaged
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (parameters.TryGetValue(identifier.Name,
                out AnimationParameter animationParameterUntyped))
            {
                if (animationParameterUntyped is
                    AnimationParameter<T> animationParameterTyped &&
                    animationParameterUntyped.Identifier.MatchesConstraint(
                        identifier))
                {
                    return animationParameterTyped;
                }
                else throw new ArgumentException("A parameter with the " +
                    "specified identifier was found, but had a different " +
                    "type then what was requested or didn't match the " +
                    "identifier type constraint.");
            }
            else throw new ArgumentException("A parameter with the specified " +
                "identifier couldn't be found.");
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator{T}"/> instance.</returns>
        public IEnumerator<AnimationParameter> GetEnumerator()
        {
            return parameters.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator"/> instance.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return parameters.Values.GetEnumerator();
        }
    }
}
