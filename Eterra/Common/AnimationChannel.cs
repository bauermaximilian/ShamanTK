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
    /// Provides the base class from which classes are derived that
    /// represent an animation of a single object parameter.
    /// </summary>
    public abstract class AnimationChannel
    {
        /// <summary>
        /// Gets the type of the keyframe values.
        /// </summary>
        public abstract Type ValueType { get; }

        /// <summary>
        /// Gets the identifier of the current channel.
        /// </summary>
        public abstract ChannelIdentifier Identifier { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimationChannel"/> 
        /// class.
        /// </summary>
        /// <param name="parentAnimation">
        /// The <see cref="Animation"/> instance this channel belongs to
        /// and takes its playback position updates from.
        /// </param>
        /// <param name="sourceChannel">
        /// The <see cref="TimelineChannel"/> instance the new
        /// <see cref="AnimationChannel"/> instance will take its keyframe
        /// data from.
        /// </param>
        /// <returns>
        /// A new <see cref="AnimationChannel"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="parentAnimation"/> or
        /// <paramref name="sourceChannel"/> are null.
        /// </exception>
        internal static AnimationChannel Create(Animation parentAnimation,
            TimelineChannel sourceChannel)
        {
            if (parentAnimation == null)
                throw new ArgumentNullException(nameof(parentAnimation));
            if (sourceChannel == null)
                throw new ArgumentNullException(nameof(sourceChannel));

            return (AnimationChannel)Activator.CreateInstance(
                typeof(AnimationChannel), parentAnimation, sourceChannel);
        }
    }

    /// <summary>
    /// Represents an animation of a single object parameter.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the object parameter values.
    /// </typeparam>
    public class AnimationChannel<T> : AnimationChannel
        where T : unmanaged
    {
        private readonly IInterpolator<T> interpolator;

        private T lastCurrentValue;

        private TimeSpan lastValueTime = TimeSpan.MinValue;

        /// <summary>
        /// Gets the minimum distance between the 
        /// <see cref="Keyframe{T}.Position"/> of two <see cref="Keyframe{T}"/>
        /// instances which needs to be exceeded so that the animated value
        /// is recalculated.
        /// </summary>
        public static TimeSpan UpdateTreshold { get; }
            = TimeSpan.FromMilliseconds(10);

        /// <summary>
        /// Gets the identifier of the current channel.
        /// </summary>
        public override ChannelIdentifier Identifier { get; }

        /// <summary>
        /// Gets the type of the keyframe values.
        /// </summary>
        public override Type ValueType { get; }        

        /// <summary>
        /// Gets the current animated value of the current layer channel.
        /// </summary>
        public ref readonly T CurrentValue
        {
            get
            {
                UpdateCurrentValue();
                return ref lastCurrentValue;
            }
        }

        private readonly Animation parentAnimation;
        private readonly TimelineChannel<T> sourceChannel;

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="AnimationChannel{T}"/> class.
        /// </summary>
        /// <param name="parentAnimation">
        /// The <see cref="Animation"/> instance this channel belongs to
        /// and takes its playback position updates from.
        /// </param>
        /// <param name="sourceChannel">
        /// The <see cref="TimelineChannel{T}"/> instance the new
        /// <see cref="AnimationChannel{T}"/> instance will take its keyframe
        /// data from.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="parentAnimation"/> or
        /// <paramref name="sourceChannel"/> are null.
        /// </exception>
        internal AnimationChannel(Animation parentAnimation,
            TimelineChannel<T> sourceChannel)
        {
            this.parentAnimation = parentAnimation ??
                throw new ArgumentNullException(nameof(parentAnimation));
            this.sourceChannel = sourceChannel ??
                throw new ArgumentNullException(nameof(sourceChannel));
            Identifier = sourceChannel.Identifier;
            ValueType = typeof(T);

            interpolator = InterpolatorProvider.GetInterpolator<T>();
            lastCurrentValue = interpolator.Default;
            UpdateCurrentValue();
        }

        private bool UpdateCurrentValue()
        {
            if (lastValueTime == TimeSpan.MinValue)
                lastValueTime = parentAnimation.Position;
            TimeSpan delta = parentAnimation.Position - lastValueTime;

            //Cover the case of animation loops or manual modifications of the
            //position of the parent player
            if (delta < TimeSpan.Zero) delta = TimeSpan.Zero - delta;

            if (delta > UpdateTreshold)
            {
                lastValueTime = parentAnimation.Position;

                if (!sourceChannel.TryFindKeyframeBefore(
                    parentAnimation.Position, 0, out Keyframe<T> x))
                    x = new Keyframe<T>(parentAnimation.Position,
                        interpolator.Default);

                if (sourceChannel.InterpolationMethod ==
                    InterpolationMethod.None)
                    lastCurrentValue = x.Value;
                else
                {
                    if (!sourceChannel.TryFindKeyframeAfter(
                        parentAnimation.Position, 0, out Keyframe<T> y))
                        y = new Keyframe<T>(parentAnimation.Position,
                            interpolator.Default);

                    float ratio = x.CalculateRatioTo(y,
                            parentAnimation.Position);

                    if (sourceChannel.InterpolationMethod ==
                        InterpolationMethod.Linear)
                    {
                        lastCurrentValue = interpolator.InterpolateLinear(
                            x.Value, y.Value, ratio);
                    }
                    else
                    {
                        if (!sourceChannel.TryFindKeyframeBefore(
                            parentAnimation.Position, -1,
                            out Keyframe<T> beforeX))
                            beforeX = x;
                        if (!sourceChannel.TryFindKeyframeAfter(
                            parentAnimation.Position, 1,
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
