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
using System.Linq;
using System.Numerics;

namespace Eterra.Common
{
    public class Entity : IEnumerable<KeyValuePair<string, object>>,  
        IDisposable
    {
        public class NameUpdatedEventArgs : EventArgs
        {
            public string PreviousName { get; }

            public Entity Entity { get; }

            public NameUpdatedEventArgs(Entity entity, string oldName)
            {
                Entity = entity ??
                    throw new ArgumentNullException(nameof(entity));
                PreviousName = oldName;
            }
        }

        public class ParameterUpdatedEventArgs : EventArgs
        {
            public bool WasRemoved => CurrentValue == null;

            public bool WasAdded => PreviousValue == null;

            public bool WasUpdated => !WasAdded && !WasRemoved;

            public string Identifier { get; }

            public object PreviousValue { get; } = null;

            public object CurrentValue { get; } = null;

            public ParameterUpdatedEventArgs(string identifier,
                object previousValue, object currentValue)
            {
                Identifier = identifier ??
                    throw new ArgumentNullException(nameof(identifier));
                PreviousValue = previousValue ??
                    throw new ArgumentNullException(nameof(previousValue));
                CurrentValue = currentValue ??
                    throw new ArgumentNullException(nameof(currentValue));
            }

            public ParameterUpdatedEventArgs(string identifier, object value)
            {
                Identifier = identifier ??
                    throw new ArgumentNullException(nameof(identifier));
                CurrentValue = value ??
                    throw new ArgumentNullException(nameof(value));
            }

            public ParameterUpdatedEventArgs(string identifier)
            {
                Identifier = identifier ??
                    throw new ArgumentNullException(nameof(identifier));
            }
        }

        private readonly Dictionary<string, object> parameters = 
            new Dictionary<string, object>();

        public ICollection<string> ParameterIdentifiers => parameters.Keys;

        public int ParameterCount => parameters.Count;

        public event EventHandler<NameUpdatedEventArgs> NameChanged;

        public event EventHandler LocationChanged;

        public event EventHandler<ParameterUpdatedEventArgs> ParameterUpdated;

        public event EventHandler<ParameterUpdatedEventArgs> ParameterRemoved;

        public event EventHandler<ParameterUpdatedEventArgs> ParameterAdded;

        public Scene Scene { get; }

        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    string oldName = name;
                    name = value;
                    NameChanged?.Invoke(this,
                        new NameUpdatedEventArgs(this, oldName));
                }
            }
        }

        public Vector3 Position
        {
            get => position;
            set
            {
                if (position != value)
                {
                    LocationChanged?.Invoke(this, EventArgs.Empty);
                    position = value;
                }
            }
        }

        public Vector3 Scale
        {
            get => scale;
            set
            {
                if (scale != value)
                {
                    LocationChanged?.Invoke(this, EventArgs.Empty);
                    scale = value;
                }
            }
        }

        public Quaternion Rotation
        {
            get => rotation;
            set
            {
                if (rotation != value)
                {
                    LocationChanged?.Invoke(this, EventArgs.Empty);
                    rotation = value;
                }
            }
        }

        private string name;
        private Vector3 position, scale;
        private Quaternion rotation;

        internal Entity(Scene parentScene)
        {
            Scene = parentScene ??
                throw new ArgumentNullException(nameof(parentScene));
        }

        public void Set(string parameterIdentifier, object value)
        {
            if (!TrySet(parameterIdentifier, value))
                throw new ArgumentException("The type of the specified " +
                    "parameter value does not match the required type for " +
                    "the specified parameter identifier.");
        }

        public void Set(EntityParameter parameterIdentifier, object value)
        {
            if (!TrySet(parameterIdentifier, value))
                throw new ArgumentException("The type of the specified " +
                    "parameter value does not match the required type for " +
                    "the specified parameter identifier.");
        }

        public bool TrySet(EntityParameter parameterIdentifier, object value)
        {
            return TrySet(parameterIdentifier.ToString(), value);
        }

        public bool TrySet(string parameterIdentifier, object value)
        {
            if (parameterIdentifier == null)
                throw new ArgumentNullException(nameof(parameterIdentifier));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (EntityParameters.SupportsType(parameterIdentifier,
                value.GetType()))
            {
                parameters.TryGetValue(parameterIdentifier,
                    out object previousValue);

                if (value != previousValue)
                {
                    parameters[parameterIdentifier] = value;

                    if (previousValue == null)
                        ParameterAdded?.Invoke(this, 
                            new ParameterUpdatedEventArgs(parameterIdentifier, 
                            value));
                    else 
                        ParameterUpdated?.Invoke(this,
                            new ParameterUpdatedEventArgs(parameterIdentifier,
                            previousValue, value));

                    return true;
                }
            }

            return false;
        }

        public bool TryGet(EntityParameter parameterIdentifier,
            out object parameterValue)
        {
            return TryGet(parameterIdentifier.ToString(), out parameterValue);
        }

        public bool TryGet(string parameterIdentifier, 
            out object parameterValue)
        {
            if (parameterIdentifier == null)
                throw new ArgumentNullException(nameof(parameterIdentifier));

            return parameters.TryGetValue(parameterIdentifier, 
                out parameterValue);
        }

        public bool TryGet<T>(EntityParameter parameterIdentifier, 
            out T parameterValue)
        {
            return TryGet<T>(parameterIdentifier.ToString(), 
                out parameterValue);
        }

        public bool TryGet<T>(string parameterIdentifier,
            out T parameterValue)
        {
            return TryGet(parameterIdentifier, out parameterValue, out _);
        }

        private bool TryGet<T>(string parameterIdentifier,
            out T parameterValue, out object parameterValueUncasted)
        {
            if (parameterIdentifier == null)
                throw new ArgumentNullException(nameof(parameterIdentifier));

            if (parameters.TryGetValue(parameterIdentifier,
                out parameterValueUncasted) &&
                typeof(T).IsAssignableFrom(parameterValueUncasted.GetType()))
            {
                parameterValue = (T)parameterValueUncasted;
                return true;
            }
            else
            {
                parameterValue = default;
                parameterValueUncasted = null;
                return false;
            }
        }

        public object Get(EntityParameter parameterIdentifier)
        {
            return Get(parameterIdentifier.ToString());
        }

        public object Get(string parameterIdentifier)
        {
            if (TryGet(parameterIdentifier, out object parameterValue))
                return parameterValue;
            else throw new ArgumentException("No parameter with the " +
                "specified identifier exists.");
        }

        public T Get<T>(EntityParameter parameterIdentifier)
        {
            return Get<T>(parameterIdentifier.ToString());
        }

        public T Get<T>(string parameterIdentifier)
        {
            if (parameterIdentifier == null)
                throw new ArgumentNullException(nameof(parameterIdentifier));

            if (TryGet(parameterIdentifier, out T parameterValue, 
                out object parameterValueUncasted))
                return parameterValue;
            else if (parameterValueUncasted != null)
                throw new ArgumentException("A parameter with the specified " +
                    "identifier was found, but couldn't be converted into " +
                    "an instance of the requested type '" + typeof(T).Name 
                    + "'.");
            else throw new ArgumentException("No parameter with the " +
                "specified identifier exists.");
        }

        public bool Remove(EntityParameter parameterIdentifier, bool dispose)
        {
            return Remove(parameterIdentifier.ToString(), dispose);
        }

        public bool Remove(string parameterIdentifier, bool dispose)
        {
            if (parameterIdentifier == null)
                throw new ArgumentNullException(nameof(parameterIdentifier));

            if (parameters.TryGetValue(parameterIdentifier, out object value))
            {
                if (dispose && value is IDisposable disposableValue)
                    disposableValue.Dispose();
                parameters.Remove(parameterIdentifier);
                ParameterRemoved?.Invoke(this, new ParameterUpdatedEventArgs(
                    parameterIdentifier));
                return true;
            }
            else return false;            
        }

        public bool Contains(string parameterIdentifier)
        {
            if (parameterIdentifier == null)
                throw new ArgumentNullException(nameof(parameterIdentifier));

            return parameters.ContainsKey(parameterIdentifier);
        }

        public bool Contains(string parameterIdentifier, Type parameterType)
        {
            if (parameterIdentifier == null)
                throw new ArgumentNullException(nameof(parameterIdentifier));
            if (parameterType == null)
                throw new ArgumentNullException(nameof(parameterType));
            
            return parameters.TryGetValue(parameterIdentifier,
                out object parameterValue) &&
                parameterType.IsAssignableFrom(parameterValue.GetType());
        }

        public bool Contains(EntityParameter parameterIdentifier)
        {
            return Contains(parameterIdentifier.ToString(),
                EntityParameters.GetEntityParameterType(parameterIdentifier));
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return parameters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return parameters.GetEnumerator();
        }

        public void Clear(bool dispose)
        {
            while (parameters.Count > 0)
                Remove(parameters.Keys.First(), true);
        }

        public void Dispose()
        {
            Clear(true);
        }

        public override string ToString()
        {
            string name;
            if (!string.IsNullOrEmpty(Name)) name = "\"" + Name + "\"";
            else name = "Unnamed entity";

            return name + " (" + ParameterCount + " parameters) at " +
                Position.ToString();
        }
    }
}
