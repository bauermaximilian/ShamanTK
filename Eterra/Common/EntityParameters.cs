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

using Eterra.IO;
using System;
using System.Collections.Generic;

namespace Eterra.Common
{
    /// <summary>
    /// Defines a set of parameters with a pre-defined value type and semantic,
    /// which is enforced by the parameter setters of <see cref="Entity"/>.
    /// This extends to the string representations of these enum values.
    /// The <see cref="EntityParameters"/> class can be used to retrieve the 
    /// associated types and verify the conformity of a value instance with a 
    /// parameter identifier.
    /// </summary>
    public enum EntityParameter
    {
        IsVisible,
        Color,
        MeshData,
        ColliderPrimitive,
        ColliderMeshData,
        Dynamics,
        TextureDataMain,
        TextureDataEffect01,
        TextureDataEffect02,
        TextureDataEffect03,
        ModelAnimationTimeline,
        TextureAnimationTimeline,
        Light
    }

    /// <summary>
    /// Provides functionality to retrieve the types associated with the
    /// <see cref="EntityParameter"/> elements and verify the conformity of a 
    /// value instance with a <see cref="EntityParameter"/>.
    /// </summary>
    public static class EntityParameters
    {
        private static readonly Dictionary<EntityParameter, Type>
            parameterTypes = new Dictionary<EntityParameter, Type>()
            {
                { EntityParameter.IsVisible, typeof(bool)},
                { EntityParameter.Color, typeof(Color) },
                { EntityParameter.ColliderMeshData, typeof(MeshData) },
                { EntityParameter.ColliderPrimitive,
                    typeof(ColliderPrimitive) },
                { EntityParameter.Dynamics, typeof(Dynamics) },
                { EntityParameter.ModelAnimationTimeline,
                    typeof(Timeline) },
                { EntityParameter.MeshData, typeof(MeshData) },
                { EntityParameter.TextureAnimationTimeline, typeof(Timeline) },
                { EntityParameter.TextureDataMain, typeof(TextureData) },
                { EntityParameter.TextureDataEffect01, typeof(TextureData) },
                { EntityParameter.TextureDataEffect02, typeof(TextureData) },
                { EntityParameter.TextureDataEffect03, typeof(TextureData) },
                { EntityParameter.Light, typeof(Light)}
            };

        static EntityParameters()
        {
            foreach (EntityParameter parameter in 
                Enum.GetValues(typeof(EntityParameter)))
            {
                if (!parameterTypes.ContainsKey(parameter))
                    throw new ApplicationException("The entity parameter " +
                        "'" + parameter.ToString() + "' doesn't have an " +
                        "associated type.");
            }
        }

        /// <summary>
        /// Checks if a specific value type can be assigned to a specific
        /// entity parameter.
        /// </summary>
        /// <param name="entityParameter">
        /// The identifier of the entity parameter which should be checked.
        /// </param>
        /// <param name="valueType">
        /// The type of the value which may (or may not) be assigned to a
        /// entity parameter with the specified identifier.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="valueType"/> can be
        /// assigned to a parameter with the specified 
        /// <paramref name="entityParameter"/> identifier (either because the 
        /// type matches or the string doesn't specify an existing 
        /// <see cref="EntityParameter"/>), <c>false</c> if the assignment is
        /// not allowed and will fail.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="entityParameter"/> or 
        /// <paramref name="valueType"/> are null.
        /// </exception>
        public static bool SupportsType(string entityParameter, Type valueType)
        {
            if (entityParameter == null)
                throw new ArgumentNullException(nameof(entityParameter));

            if (Enum.TryParse(entityParameter, out EntityParameter parameter))
                return SupportsType(parameter, valueType);
            else return true;
        }

        /// <summary>
        /// Checks if a specific value type can be assigned to a specific
        /// entity parameter.
        /// </summary>
        /// <param name="parameter">
        /// The identifier of the entity parameter to be checked.
        /// </param>
        /// <param name="valueType">
        /// The type of the value which may (or may not) be assigned to a
        /// entity parameter with the specified identifier.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="valueType"/> can be
        /// assigned to a parameter with the specified 
        /// <paramref name="entityParameter"/> identifier, <c>false</c> if the 
        /// assignment is not allowed and will fail.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="valueType"/> is null.
        /// </exception>
        public static bool SupportsType(EntityParameter parameter, 
            Type valueType)
        {
            if (parameterTypes.TryGetValue(parameter, out Type parameterType))
                return parameterType.IsAssignableFrom(valueType);
            else return false;
        }

        /// <summary>
        /// Retrieves the pre-defined <see cref="Type"/> associated to a entity
        /// parameter identifier.
        /// </summary>
        /// <param name="entityParameter">
        /// The identifier of the entity parameter.
        /// </param>
        /// <param name="valueType">
        /// The associated value type or null, if the method returns 
        /// <c>false</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> when the specified <paramref name="entityParameter"/>
        /// identifier has a fixed type associated to it that was stored in
        /// the <paramref name="valueType"/> parameter, <c>false</c> if the
        /// specified <paramref name="entityParameter"/> doesn't have a fixed
        /// <see cref="Type"/> associated to it.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="entityParameter"/> is null.
        /// </exception>
        public static bool TryGetEntityParameterType(string entityParameter,
            out Type valueType)
        {
            if (entityParameter == null)
                throw new ArgumentNullException(nameof(entityParameter));

            if (Enum.TryParse(entityParameter, out EntityParameter parameter))
                return parameterTypes.TryGetValue(parameter, out valueType);
            else
            {
                valueType = null;
                return false;
            }
        }

        /// <summary>
        /// Retrieves the pre-defined <see cref="Type"/> associated to a entity
        /// parameter identifier.
        /// </summary>
        /// <param name="parameter">
        /// The identifier of the entity parameter.
        /// </param>
        /// <returns>
        /// The associated value <see cref="Type"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="parameter"/> is
        /// invalid.
        /// </exception>
        public static Type GetEntityParameterType(EntityParameter parameter)
        {
            if (!Enum.IsDefined(typeof(EntityParameter), parameter))
                throw new ArgumentException("The specified entity " +
                    "parameter identifier is invalid.");

            if (parameterTypes.TryGetValue(parameter, out Type valueType))
                return valueType;
            else throw new ArgumentException("The specified entity " +
                "parameter identifier is not defined.");
        }

        /// <summary>
        /// Retrieves the pre-defined <see cref="Type"/> associated to a entity
        /// parameter identifier.
        /// </summary>
        /// <param name="entityParameter">
        /// The identifier of the entity parameter.
        /// </param>
        /// <returns>
        /// The associated value <see cref="Type"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="entityParameter"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="parameter"/> is
        /// no defined <see cref="EntityParameter"/>.
        /// </exception>
        public static Type GetEntityParameterType(string entityParameter)
        {
            if (entityParameter == null)
                throw new ArgumentNullException(nameof(entityParameter));

            if (TryGetEntityParameterType(entityParameter, out Type type))
                return type;
            else throw new ArgumentException("The specified entity " +
                "parameter identifier is not defined.");
        }
    }
}
