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

namespace Eterra.Common
{
    /// <summary>
    /// Provides access to the current animated value of a single 
    /// <see cref="TimelineLayer{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The value type of the associated <see cref="TimelineLayer{T}"/>.
    /// </typeparam>
    public class AnimationPlayerLayer<T> : IAnimationLayer<T>
        where T : unmanaged
    {
        private readonly AnimationPlayer parentPlayer;

        /// <summary>
        /// Gets the identifier of the layer, which is used by the current
        /// <see cref="AnimationPlayerLayer"/>.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Gets the value type of the associated <see cref="TimelineLayer"/>.
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        /// Gets the minimum distance between the 
        /// <see cref="Keyframe{T}.Position"/> of two <see cref="Keyframe{T}"/>
        /// instances which needs to be exceeded so that the animated value
        /// is recalculated.
        /// </summary>
        public static TimeSpan UpdateTreshold { get; }
            = TimeSpan.FromMilliseconds(10);

        private readonly TimelineLayer<T> sourceTimelineLayer;
        private readonly IInterpolator<T> interpolator;

        private T lastCurrentValue;

        private TimeSpan lastValueTime = TimeSpan.MinValue;

        /// <summary>
        /// Gets the current animated value of the current layer.
        /// </summary>
        public ref readonly T CurrentValue
        {
            get
            {
                UpdateCurrentValue();
                return ref lastCurrentValue;
            }
        }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="AnimationPlayerLayer{T}"/> class.
        /// </summary>
        /// <param name="parentPlayer">The parent player.</param>
        /// <param name="layerIdentifier">
        /// The identifier of the timeline layer in 
        /// <paramref name="timeline"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="parentPlayer"/> or
        /// <paramref name="layerIdentifier"/> are null.
        /// </exception>
        public AnimationPlayerLayer(AnimationPlayer parentPlayer, 
            string layerIdentifier)
        {
            this.parentPlayer = parentPlayer ??
                throw new ArgumentNullException(nameof(parentPlayer));
            Identifier = layerIdentifier ??
                throw new ArgumentNullException(nameof(layerIdentifier));
            ValueType = typeof(T);

            sourceTimelineLayer =
                this.parentPlayer.Timeline.GetLayer<T>(Identifier);

            interpolator = InterpolatorProvider.GetInterpolator<T>();
            lastCurrentValue = interpolator.Default;
            UpdateCurrentValue();
        }
        
        private bool UpdateCurrentValue()
        {
            if (lastValueTime == TimeSpan.MinValue)
                lastValueTime = parentPlayer.Position;
            TimeSpan delta = parentPlayer.Position - lastValueTime;

            //Cover the case of animation loops or manual modifications of the
            //position of the parent player
            if (delta < TimeSpan.Zero) delta = TimeSpan.Zero - delta;

            if (delta > UpdateTreshold)
            {
                lastValueTime = parentPlayer.Position;

                if (!sourceTimelineLayer.TryFindKeyframeBefore(
                    parentPlayer.Position, 0, out Keyframe<T> x))
                    x = new Keyframe<T>(parentPlayer.Position,
                        interpolator.Default);

                if (sourceTimelineLayer.InterpolationMethod == 
                    InterpolationMethod.None)
                    lastCurrentValue = x.Value;
                else
                {
                    if (!sourceTimelineLayer.TryFindKeyframeAfter(
                        parentPlayer.Position, 0, out Keyframe<T> y))
                        y = new Keyframe<T>(parentPlayer.Position,
                            interpolator.Default);

                    float ratio = x.CalculateRatioTo(y,
                            parentPlayer.Position);

                    if (sourceTimelineLayer.InterpolationMethod == 
                        InterpolationMethod.Linear)
                    {
                        lastCurrentValue = interpolator.InterpolateLinear(
                            x.Value, y.Value, ratio);
                    }
                    else
                    {
                        if (!sourceTimelineLayer.TryFindKeyframeBefore(
                            parentPlayer.Position, -1,
                            out Keyframe<T> beforeX))
                            beforeX = x;
                        if (!sourceTimelineLayer.TryFindKeyframeAfter(
                            parentPlayer.Position, 1,
                            out Keyframe<T> afterY))
                            afterY = y;

                        lastCurrentValue = interpolator.InterpolateCubic(
                            beforeX.Value, x.Value, y.Value, afterY.Value,
                            ratio);
                    }
                }

                return true;
            }
            else return false;
        }
    }
}
