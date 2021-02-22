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
using System.Reflection;

namespace ShamanTK.Common
{
    /// <summary>
    /// Provides the base class from which classes are derived that
    /// represent an animation of a single object parameter.
    /// </summary>
    public abstract class AnimationParameter
    {
        /// <summary>
        /// Gets the type of the keyframe values.
        /// </summary>
        public abstract Type ValueType { get; }

        /// <summary>
        /// Gets the identifier of the current instance.
        /// </summary>
        public abstract ParameterIdentifier Identifier { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimationParameter"/> 
        /// class.
        /// </summary>
        /// <param name="parentAnimation">
        /// The <see cref="Animation"/> instance this parameter belongs to
        /// and takes its playback position updates from.
        /// </param>
        /// <param name="sourceParameter">
        /// The <see cref="TimelineParameter"/> instance the new
        /// <see cref="AnimationParameter"/> instance will take its keyframe
        /// data from.
        /// </param>
        /// <returns>
        /// A new <see cref="AnimationParameter"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="parentAnimation"/> or
        /// <paramref name="sourceParameter"/> are null.
        /// </exception>
        internal static AnimationParameter Create(Animation parentAnimation,
            TimelineParameter sourceParameter)
        {
            if (parentAnimation == null)
                throw new ArgumentNullException(nameof(parentAnimation));
            if (sourceParameter == null)
                throw new ArgumentNullException(nameof(sourceParameter));

            Type animationParameterType = 
                typeof(AnimationParameter<>).MakeGenericType(
                    sourceParameter.ValueType);

            ConstructorInfo constructor =
                animationParameterType.GetConstructor(new Type[] { 
                    typeof(Animation), sourceParameter.GetType() });

            return (AnimationParameter)constructor.Invoke(new object[] {
                parentAnimation, sourceParameter});
        }
    }

    /// <summary>
    /// Represents an animation of a single object parameter.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the object parameter values.
    /// </typeparam>
    public class AnimationParameter<T> : AnimationParameter
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
        /// Gets the identifier of the current parameter.
        /// </summary>
        public override ParameterIdentifier Identifier { get; }

        /// <summary>
        /// Gets the type of the keyframe values.
        /// </summary>
        public override Type ValueType { get; }

        /// <summary>
        /// Gets the current animated value of the current layer parameter.
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
        private readonly TimelineParameter<T> sourceParameter;

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="AnimationParameter{T}"/> class.
        /// </summary>
        /// <param name="parentAnimation">
        /// The <see cref="Animation"/> instance this parameter belongs to
        /// and takes its playback position updates from.
        /// </param>
        /// <param name="sourceParameter">
        /// The <see cref="TimelineParameter{T}"/> instance the new
        /// <see cref="AnimationParameter{T}"/> instance will take its keyframe
        /// data from.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="parentAnimation"/> or
        /// <paramref name="sourceParameter"/> are null.
        /// </exception>
        public AnimationParameter(Animation parentAnimation,
            TimelineParameter<T> sourceParameter)
        {
            this.parentAnimation = parentAnimation ??
                throw new ArgumentNullException(nameof(parentAnimation));
            this.sourceParameter = sourceParameter ??
                throw new ArgumentNullException(nameof(sourceParameter));
            Identifier = sourceParameter.Identifier;
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

                if (!sourceParameter.TryFindKeyframeBefore(
                    parentAnimation.Position, 0, out Keyframe<T> x))
                    x = new Keyframe<T>(parentAnimation.Position,
                        interpolator.Default);

                if (sourceParameter.InterpolationMethod ==
                    InterpolationMethod.None)
                    lastCurrentValue = x.Value;
                else
                {
                    if (!sourceParameter.TryFindKeyframeAfter(
                        parentAnimation.Position, 0, out Keyframe<T> y))
                        y = new Keyframe<T>(parentAnimation.Position,
                            interpolator.Default);

                    float ratio = x.CalculateRatioTo(y,
                            parentAnimation.Position);

                    if (sourceParameter.InterpolationMethod ==
                        InterpolationMethod.Linear)
                    {
                        lastCurrentValue = interpolator.InterpolateLinear(
                            x.Value, y.Value, ratio);
                    }
                    else
                    {
                        if (!sourceParameter.TryFindKeyframeBefore(
                            parentAnimation.Position, -1,
                            out Keyframe<T> beforeX))
                            beforeX = x;
                        if (!sourceParameter.TryFindKeyframeAfter(
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
