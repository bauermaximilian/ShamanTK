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
using System.Collections.Generic;

namespace Eterra.Common
{
    /// <summary>
    /// Represents an animation with multiple layers and value types.
    /// </summary>
    public interface IAnimation
    {
        /// <summary>
        /// Gets a collection of every <see cref="IAnimationLayer"/> in the 
        /// current <see cref="IAnimation"/> instance.
        /// </summary>
        ICollection<IAnimationLayer> Layers { get; }

        /// <summary>
        /// Gets a collection of the identifiers of every
        /// <see cref="IAnimationLayer"/> in the current
        /// <see cref="IAnimation"/> instance.
        /// </summary>
        ICollection<string> LayerIdentifiers { get; }

        /// <summary>
        /// Gets a <see cref="IAnimationLayer"/> instance from the current
        /// <see cref="IAnimation"/> instance.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the <see cref="IAnimationLayer"/>
        /// </param>
        /// <returns>
        /// The requested <see cref="IAnimationLayer"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="IAnimationLayer"/> with the 
        /// specified <paramref name="identifier"/> was found in the current
        /// <see cref="IAnimation"/>.
        /// </exception>
        IAnimationLayer GetLayer(string identifier);

        /// <summary>
        /// Gets a <see cref="IAnimationLayer{T}"/> instance from the current
        /// <see cref="IAnimation"/> instance.
        /// </summary>
        /// <typeparam name="T">
        /// The value type of the <see cref="IAnimationLayer{T}"/>.
        /// </typeparam>
        /// <param name="identifier">
        /// The identifier of the <see cref="IAnimationLayer{T}"/>
        /// </param>
        /// <returns>
        /// The requested <see cref="IAnimationLayer{T}"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="IAnimationLayer{T}"/> with the 
        /// specified <paramref name="identifier"/> was found in the current
        /// <see cref="IAnimation"/>.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// Is thrown when the specified <typeparamref name="T"/> doesn't
        /// match the type of the requested 
        /// <see cref="IAnimationLayer{T}"/>.
        /// </exception>
        IAnimationLayer<T> GetLayer<T>(string identifier)
            where T : unmanaged;

        /// <summary>
        /// Updates the animation.
        /// </summary>
        /// <param name="delta">
        /// The amount of time elapsed since the last update.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="delta"/> is negative.
        /// </exception>
        void Update(TimeSpan delta);
    }

    /// <summary>
    /// Provides access to a single layer of an <see cref="IAnimation"/>.
    /// </summary>
    public interface IAnimationLayer
    {
        /// <summary>
        /// Gets the type of the keyframe values.
        /// </summary>
        Type ValueType { get; }

        /// <summary>
        /// Gets the identifier of the current instance.
        /// </summary>
        string Identifier { get; }
    }

    /// <summary>
    /// Provides access to a single layer of an <see cref="IAnimation"/>.
    /// </summary>
    /// <typeparam name="T">The type of the animated value.</typeparam>
    public interface IAnimationLayer<T> : IAnimationLayer where T : unmanaged
    {
        /// <summary>
        /// Gets the current animated value of the current layer.
        /// </summary>
        ref readonly T CurrentValue { get; }
    }
}
