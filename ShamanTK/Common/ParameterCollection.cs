using System;
using System.Collections.Generic;
using System.Linq;

namespace ShamanTK.Common
{
    /// <summary>
    /// Represents a collection of parameters of varying types with a 
    /// <see cref="ParameterIdentifier"/> as key.
    /// </summary>
    public class ParameterCollection : IDictionary<ParameterIdentifier, object>
    {
        private readonly Dictionary<ParameterIdentifier, object> data
            = new Dictionary<ParameterIdentifier, object>();

        /// <summary>
        /// Gets the name of the current <see cref="ParameterCollection"/> 
        /// instance or null.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <returns>
        /// The value associated with the specified key. If the specified key 
        /// is not found, a get operation throws a 
        /// <see cref="KeyNotFoundException"/>, and a set operation creates 
        /// a new element with the specified key.
        /// </returns>
        public object this[ParameterIdentifier key]
        {
            get => data[key];
            set
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                if (key.MatchesConstraint(value)) data[key] = value;
                else throw new ArgumentException("The specified value type " +
                    "doesn't match the type constraint of the specified " +
                    "parameter identifier.");
            }
        }

        /// <summary>
        /// Gets a collection containing the keys in the current 
        /// <see cref="ParameterCollection"/> instance.
        /// </summary>
        public ICollection<ParameterIdentifier> Keys => data.Keys;

        /// <summary>
        /// Gets a collection containing the values in the current 
        /// <see cref="ParameterCollection"/> instance.
        /// </summary>
        public ICollection<object> Values => data.Values;

        /// <summary>
        /// Gets the number of key/value pairs contained in the current
        /// <see cref="ParameterCollection"/> instance.
        /// </summary>
        public int Count => data.Count;

        /// <summary>
        /// Gets a value indicating whether the current 
        /// <see cref="ParameterCollection"/> instance is read only 
        /// (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Initializes a new unnamed instance of the 
        /// <see cref="ParameterCollection"/> class.
        /// </summary>
        public ParameterCollection() { }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="ParameterCollection"/> class.
        /// </summary>
        /// <param name="name">
        /// The name of the new <see cref="ParameterCollection"/> instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="name"/> is null.
        /// </exception>
        public ParameterCollection(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Adds the specified parameter to the 
        /// <see cref="ParameterCollection"/>.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="key"/> or <paramref name="value"/> 
        /// are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="value"/> doesn't match
        /// the constraint specified through the <paramref name="key"/>.
        /// </exception>
        public void Add(ParameterIdentifier key, object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (key.MatchesConstraint(value)) data[key] = value;
            else throw new ArgumentException("The specified value type " +
                "doesn't match the type constraint of the specified " +
                "parameter identifier.");

            data.Add(key, value);
        }

        /// <summary>
        /// Adds the specified parameter to the 
        /// <see cref="ParameterCollection"/>.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when the key or the value of
        /// of <paramref name="item"/> are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the value specified in <paramref name="item"/> 
        /// doesn't match the constraint specified through the key.
        /// </exception>
        public void Add(KeyValuePair<ParameterIdentifier, object> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Removes all parameters from the <see cref="ParameterCollection"/>.
        /// </summary>
        public void Clear()
        {
            data.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="ParameterCollection"/> contains
        /// the specified item.
        /// </summary>
        /// <param name="item">
        /// The item to locate in the <see cref="ParameterCollection"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <see cref="ParameterCollection"/> contains an 
        /// element with the specified item; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="item"/> is null.
        /// </exception>
        public bool Contains(KeyValuePair<ParameterIdentifier, object> item)
        {
            return data.Contains(item);
        }

        /// <summary>
        /// Determines whether the <see cref="ParameterCollection"/> contains 
        /// the specified key.
        /// </summary>
        /// <param name="key">
        /// The key to locate in the <see cref="ParameterCollection"/>
        /// </param>
        /// <returns>
        /// <c>true</c> if the <see cref="ParameterCollection"/> contains an 
        /// element with the specified key; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="key"/> is null.
        /// </exception>
        public bool ContainsKey(ParameterIdentifier key)
        {
            return data.ContainsKey(key);
        }

        /// <summary>
        /// Copies the <see cref="ParameterCollection"/> or a portion of it 
        /// to an array.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of 
        /// the elements copied from <see cref="ParameterCollection"/>. 
        /// The Array must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in <paramref name="array"/> at which 
        /// copying begins.
        /// </param>
        public void CopyTo(KeyValuePair<ParameterIdentifier, object>[] array,
            int arrayIndex)
        {
            ((ICollection<KeyValuePair<ParameterIdentifier, object>>)data)
                .CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the value with the specified key from the 
        /// <see cref="ParameterCollection"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// <c>true</c> if the element is successfully found and removed; 
        /// otherwise, <c>false</c>. This method returns false if 
        /// <paramref name="key"/> is not found in the 
        /// <see cref="ParameterCollection"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="key"/> is null.
        /// </exception>
        public bool Remove(ParameterIdentifier key)
        {
            return data.Remove(key);
        }

        /// <summary>
        /// Removes the specified item from the
        /// <see cref="ParameterCollection"/>.
        /// </summary>
        /// <param name="item">The item to be removed.</param>
        /// <returns>
        /// <c>true</c> if the item is successfully found and removed; 
        /// otherwise, <c>false</c>. This method returns false if 
        /// <paramref name="item"/> is not found in the 
        /// <see cref="ParameterCollection"/>.
        /// </returns>
        public bool Remove(KeyValuePair<ParameterIdentifier, object> item)
        {
            return data.Remove(item.Key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">
        /// When this method returns <c>true</c>, contains the value 
        /// associated with the specified key, if the key is found; otherwise, 
        /// null. This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <see cref="ParameterCollection"/> contains an 
        /// element with the specified key; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="key"/> is null.
        /// </exception>
        public bool TryGetValue(ParameterIdentifier key, out object value)
        {
            return data.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">
        /// When this method returns <c>true</c>, contains the value 
        /// associated with the specified key, if the key is found and the 
        /// value is of type <typeparamref name="T"/>; otherwise, null. 
        /// This parameter is passed uninitialized.
        /// </param>
        /// <typeparam name="T">
        /// The type the <paramref name="value"/> should be returned as.
        /// </typeparam>
        /// <returns>
        /// <c>true</c> if the <see cref="ParameterCollection"/> contains an 
        /// element with the specified <paramref name="key"/> and 
        /// type <typeparamref name="T"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="key"/> is null.
        /// </exception>
        public bool TryGetValue<T>(ParameterIdentifier key, out T value)
        {
            bool valueFound = TryGetValue(key, out object valueUntyped);

            if (valueFound && valueUntyped is T valueTyped)
            {
                value = valueTyped;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator{T}"/> instance.</returns>
        public IEnumerator<KeyValuePair<ParameterIdentifier, object>>
            GetEnumerator()
        {
            return data.GetEnumerator();
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> for the 
        /// current instance.
        /// </summary>
        /// <returns>A new <see cref="IEnumerator"/> instance.</returns>
        System.Collections.IEnumerator
            System.Collections.IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name)) return $"{Count} parameters";
            else return $"\"{Name}\" ({Count} parameters)";
        }
    }
}
