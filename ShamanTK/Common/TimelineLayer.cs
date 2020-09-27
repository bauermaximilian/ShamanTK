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
    /// Provides a collection of <see cref="Keyframe"/> instances, 
    /// hierarchically structured into contained 
    /// <see cref="TimelineParameter"/> instances per
    /// object parameter.
    /// </summary>
    public class TimelineLayer : IEnumerable<TimelineParameter>
    {
        private readonly Dictionary<string, TimelineParameter> parameters =
            new Dictionary<string, TimelineParameter>();

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
        /// Gets the amount of parameters.
        /// </summary>
        public int ParameterCount => parameters.Count;

        /// <summary>
        /// Gets a value indicating whether the current instance contains at 
        /// least one <see cref="TimelineParameter"/> that contains at least one 
        /// <see cref="Keyframe"/> (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool HasKeyframes { get; }

        /// <summary>
        /// Gets a value indicating whether the current instance contains at 
        /// least one <see cref="TimelineParameter"/> (<c>true</c>) or not 
        /// (<c>false</c>).
        /// </summary>
        public bool HasParameters { get; }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="TimelineLayer"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the new <see cref="TimelineLayer"/> instance.
        /// </param>
        /// <param name="parameters">
        /// An enumeration of <see cref="TimelineParameter"/> instances,
        /// which will be held by the new <see cref="TimelineLayer"/>
        /// instance.
        /// </param>
        public TimelineLayer(string identifier, 
            IEnumerable<TimelineParameter> parameters)
        {
            Identifier = identifier ??
                throw new ArgumentNullException(nameof(identifier));

            foreach (TimelineParameter parameter in parameters)
            {
                if (parameter == null) continue;
                if (this.parameters.ContainsKey(parameter.Identifier.Name))
                    throw new ArgumentException("The enumeration of " +
                        "timeline parameters contains at least two instances " +
                        "with the same identifier.");
                else this.parameters[parameter.Identifier.Name] = parameter;
            }

            TimeSpan start = TimeSpan.MaxValue;
            TimeSpan end = TimeSpan.MinValue;

            HasKeyframes = false;
            HasParameters = false;

            foreach (TimelineParameter parameter in parameters)
            {
                HasParameters = true;
                HasKeyframes |= parameter.HasKeyframes;

                if (parameter.Start < start) start = parameter.Start;
                if (parameter.End > end) end = parameter.End;                
            }

            //The position of the first marker or keyframe.
            Start = start != TimeSpan.MaxValue ? start : TimeSpan.Zero;
            //The position of the last marker or keyframe.
            End = end != TimeSpan.MinValue ? end : TimeSpan.Zero;
            //The distance between the start and the end alias timeline length.
            Length = End - Start;
        }

        /// <summary>
        /// Gets a <see cref="TimelineParameter{T}"/> from the current instance.
        /// </summary>
        /// <typeparam name="T">
        /// The value type of the keyframes in the 
        /// <see cref="TimelineParameter{T}"/>.
        /// </typeparam>
        /// <param name="parameterIdentifier">
        /// The parameter identifier of the <see cref="TimelineParameter{T}"/>
        /// to be returned.
        /// </param>
        /// <returns>
        /// The requested <see cref="TimelineParameter{T}"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="parameterIdentifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="TimelineParameter{T}"/> with the
        /// specified <paramref name="parameterIdentifier"/> was found, when
        /// the type constraint in the specfieid 
        /// <paramref name="parameterIdentifier"/> doesn't match with the 
        /// <see cref="TimelineParameter.ValueType"/> of the 
        /// <see cref="TimelineParameter{T}"/> or when
        /// the type specified through <typeparamref name="T"/> doesn't match
        /// with the type of the <see cref="TimelineParameter{T}"/>.
        /// </exception>
        public TimelineParameter<T> GetParameter<T>(
            ParameterIdentifier parameterIdentifier)
            where T : unmanaged
        {
            if (parameterIdentifier == null)
                throw new ArgumentNullException(nameof(parameterIdentifier));

            if (parameters.TryGetValue(parameterIdentifier.Name,
                out TimelineParameter parameter))
            {
                if (parameterIdentifier.ValueTypeConstraint.IsAssignableFrom(
                    parameter.Identifier.ValueTypeConstraint))
                {
                    if (parameter is TimelineParameter<T> typedParameter)
                        return typedParameter;
                    else throw new ArgumentException("A parameter with the " +
                        "specified identifier was found, but couldn't be " +
                        "converted to the specified type.");
                }
                else throw new ArgumentException("A parameter with the " +
                    "specified identifier name was found, but with an " +
                    "incompatible type.");
            }
            else throw new ArgumentException("A parameter with the " +
              "specified identifier name couldn't be found.");
        }

        /// <summary>
        /// Gets a <see cref="TimelineParameter"/> from the current instance.
        /// </summary>
        /// <param name="parameterIdentifier">
        /// The parameter name of the <see cref="TimelineParameter"/>
        /// to be returned.
        /// </param>
        /// <returns>
        /// The requested <see cref="TimelineParameter"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="parameterIdentifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when no <see cref="TimelineParameter"/> with the
        /// specified <paramref name="parameterIdentifier"/> was found or when
        /// the type constraint in the specfieid 
        /// <paramref name="parameterIdentifier"/> doesn't match with the 
        /// <see cref="TimelineParameter.ValueType"/> of the 
        /// <see cref="TimelineParameter"/>.
        /// </exception>
        public TimelineParameter GetParameter(
            ParameterIdentifier parameterIdentifier)
        {
            if (parameterIdentifier == null)
                throw new ArgumentNullException(nameof(parameterIdentifier));

            if (parameters.TryGetValue(parameterIdentifier.Name,
                out TimelineParameter parameter))
            {
                if (parameterIdentifier.ValueTypeConstraint.IsAssignableFrom(
                    parameter.ValueType))
                {
                    return parameter;
                }
                else throw new ArgumentException("A parameter with the " +
                    "specified identifier name was found, but with an " +
                    "incompatible type.");
            }
            else throw new ArgumentException("A parameter with the " +
              "specified identifier name couldn't be found.");
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator{T}"/> instance.</returns>
        public IEnumerator<TimelineParameter> GetEnumerator()
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

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"\"{Identifier}\" (Parameters: {ParameterCount}, " +
                $"Length: {Length})";
        }
    }
}
