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

namespace ShamanTK.Common
{
    /// <summary>
    /// Defines the available interpolation methods for animating values.
    /// </summary>
    public enum InterpolationMethod
    {
        /// <summary>
        /// Doesn't perform any interpolation between keyframes.
        /// </summary>
        None = 0,
        /// <summary>
        /// Perform a linear interpolation between the two keyframes around
        /// the current playback position, which produces direct and linear 
        /// transitions between keyframes and firm directional changes on
        /// the keyframe positions.
        /// </summary>
        Linear = 1,
        /// <summary>
        /// Performs a cubic interpolation using the four keyframes around 
        /// the current playback position, which produces a smooth transition 
        /// between keyframes with soft directional changes on the keyframe
        /// positions.
        /// </summary>
        Cubic = 2,
    }

    /// <summary>
    /// Provides functionality to animate values of a certain type.
    /// </summary>
    /// <typeparam name="T">
    /// The (value) type which can be animated with this animator.
    /// </typeparam>
    public interface IInterpolator<T> where T : unmanaged
    {
        /// <summary>
        /// Gets a readonly reference to the default value.
        /// </summary>
        ref readonly T Default { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="ratioXY"></param>
        /// <returns></returns>
        T InterpolateLinear(in T x, in T y, float ratioXY);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="beforeX"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="afterY"></param>
        /// <param name="ratioXY"></param>
        /// <returns></returns>
        T InterpolateCubic(in T beforeX, in T x, in T y, in T afterY,
            float ratioXY);
    }
}
