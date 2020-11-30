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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace ShamanTK.IO
{
    /// <summary>
    /// Represents the name of a resource within a resource file.
    /// </summary>
    public readonly struct ResourceQuery : IEquatable<ResourceQuery>
    {
        /// <summary>
        /// Defines the character token, which is used to separate a name
        /// from its value in a <see cref="ResourceQuery"/> instance which 
        /// specifies a name-value-collection.
        /// </summary>
        public const char NameValueSeparator = '=';

        /// <summary>
        /// Defines the character token, which is used to separate name-value
        /// pairs in a <see cref="ResourceQuery"/> instance which specifies a 
        /// name-value-collection.
        /// </summary>
        public const char ValuePairSeparator = '&';

        /// <summary>
        /// Gets an empty <see cref="ResourceQuery"/> instance.
        /// </summary>
        public static ResourceQuery Empty { get; }
            = new ResourceQuery(string.Empty);

        /// <summary>
        /// Gets the string representation of the current 
        /// <see cref="ResourceQuery"/> instance.
        /// To retrieve a string with all encoded characters decoded, use 
        /// <see cref="GetDecodedPathString"/>. 
        /// </summary>
        public string QueryString => queryString ?? "";
        private readonly string queryString;

        /// <summary>
        /// Gets a boolean which indicates whether the current path is empty
        /// (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool IsEmpty => QueryString.Length == 0;

        /// <summary>
        /// Initializes a new <see cref="ResourceQuery"/> instance.
        /// </summary>
        /// <param name="queryString">
        /// The string of the query. Unsupported characters are encoded.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="queryString"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="queryString"/> contains the
        /// <see cref="ResourcePath.ElementSeparator"/>.
        /// </exception>
        public ResourceQuery(string queryString)
        {
            if (queryString == null)
                throw new ArgumentNullException(nameof(queryString));

            string trimmedValue = queryString.Trim();

            if (!trimmedValue.Contains(
                ResourcePath.ElementSeparator.ToString()))
                this.queryString = trimmedValue;
            else throw new ArgumentException("The specified query string " +
                    "contains the element separator character '" + 
                    ResourcePath.ElementSeparator + "', which is forbidden.");
        }

        /// <summary>
        /// Initializes a new <see cref="ResourceQuery"/> instance.
        /// Both the <paramref name="queryKey"/>, <paramref name="queryValue"/>
        /// and the remaining <paramref name="queryKeyValueStrings"/>
        /// are converted to their escaped representation automatically.
        /// </summary>
        /// <param name="queryKey">The first query parameter key.</param>
        /// <param name="queryValue">The first query parameter value.</param>
        /// <param name="queryKeyValueStrings">
        /// Optional, additional keys and values (two string parameters for
        /// each key-value-tuple). Must have an even amount of elements
        /// or be left empty.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="queryKey"/> or
        /// <paramref name="queryValue"/> are null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="queryKeyValueStrings"/> is not null 
        /// and the amount of elements in the array is uneven, which implies
        /// that a key without a value was defined.
        /// </exception>
        public ResourceQuery(string queryKey, string queryValue, 
            params string[] queryKeyValueStrings)
        {
            if (queryKey == null)
                throw new ArgumentNullException(nameof(queryKey));
            if (queryValue == null)
                throw new ArgumentNullException(nameof(queryValue));

            NameValueCollection parameters = new NameValueCollection
            {
                { queryKey, queryValue }
            };

            if (queryKeyValueStrings != null)
                if (queryKeyValueStrings.Length % 2 != 0)
                    throw new ArgumentException("The amount of specified " +
                        "additional key value strings is uneven.");
                else for (int i = 0; i < queryKeyValueStrings.Length; i+=2)
                        parameters.Add(queryKeyValueStrings[i],
                            queryKeyValueStrings[i + 1]);

            queryString = BuildQueryString(parameters);
        }

        /// <summary>
        /// Initializes a new <see cref="ResourceQuery"/> instance.
        /// Both the names and values are converted to their escaped 
        /// representation automatically.
        /// </summary>
        /// <param name="queryParameters">
        /// A collection of name-value pairs, which should be used to generate
        /// a query string.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="queryParameters"/> is null.
        /// </exception>
        public ResourceQuery(NameValueCollection queryParameters)
        {
            if (queryParameters == null)
                throw new ArgumentNullException(nameof(queryParameters));

            queryString = BuildQueryString(queryParameters);
        }

        private static string BuildQueryString(NameValueCollection parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            StringBuilder stringBuilder = new StringBuilder();
            foreach (string key in parameters.Keys)
            {
                if (key == null)
                    stringBuilder.Insert(0, parameters.Get(null));
                else
                {
                    if (stringBuilder.Length > 0)
                        stringBuilder.Append(ValuePairSeparator);
                    stringBuilder.Append(Uri.EscapeDataString(key));
                    stringBuilder.Append(NameValueSeparator);
                    stringBuilder.Append(Uri.EscapeDataString(
                        parameters.Get(key) ?? ""));
                }
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Initializes a new <see cref="ResourceQuery"/> instance.
        /// </summary>
        /// <param name="path">
        /// A <see cref="FileSystemPath"/> instance to be converted to a
        /// query string.
        /// </param>
        /// <param name="omitRootToken">
        /// <c>true</c> to omit the root token of the <paramref name="path"/>
        /// (if present), so that the query appears like a relative 
        /// <see cref="FileSystemPath"/>, <c>false</c> to use the 
        /// unmodified <paramref name="path"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="path"/> is null.
        /// </exception>
        public ResourceQuery(FileSystemPath path, bool omitRootToken = true)
        {
            if (omitRootToken) queryString = path.PathString.TrimStart(
                FileSystemPath.SeparatorPathElement);
            else queryString = path.PathString;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return QueryString;
        }

        /// <summary>
        /// Parses the current <see cref="QueryString"/> as a collection of 
        /// names and values, while both are decoded in the process.
        /// </summary>
        /// <returns>
        /// A new <see cref="NameValueCollection"/> instance.
        /// </returns>
        public NameValueCollection ToNameValueCollection()
        {
            NameValueCollection collection = new NameValueCollection();

            foreach (string nameValuePair in
                QueryString.Split(ValuePairSeparator))
            {
                if (nameValuePair.Length > 0)
                {
                    int separatorIndex = nameValuePair.IndexOf(
                        NameValueSeparator);
                    if (separatorIndex == 0 && nameValuePair.Length > 1)
                        collection.Add(null, Uri.UnescapeDataString(
                            nameValuePair.Substring(1, 
                            nameValuePair.Length - 1)));
                    else if (separatorIndex > 0)
                    {
                        collection.Add(Uri.UnescapeDataString(
                            nameValuePair.Substring(0, separatorIndex)), 
                            Uri.UnescapeDataString(nameValuePair.Substring(
                                separatorIndex + 1, nameValuePair.Length -
                                separatorIndex - 1)));
                    }
                }
            }
            return collection;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current 
        /// object. 
        /// </summary>
        /// <param name="obj">
        /// The object to compare with the current object.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified object is equal to the current object;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is ResourceQuery && Equals((ResourceQuery)obj);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object 
        /// of the same type. 
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// <c>true</c> if the current object is equal to the 
        /// <paramref name="other"/> parameter; otherwise, <c>false</c>. 
        /// </returns>
        public bool Equals(ResourceQuery other)
        {
            return QueryString == other.QueryString;
        }

        /// <summary>
        /// Calculates the hash of the current instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return 1432394582 + EqualityComparer<string>.Default.GetHashCode(
                QueryString);
        }

        public static bool operator ==(ResourceQuery query1, 
            ResourceQuery query2)
        {
            return query1.Equals(query2);
        }

        public static bool operator !=(ResourceQuery query1, 
            ResourceQuery query2)
        {
            return !(query1 == query2);
        }

        public static implicit operator string(ResourceQuery query)
        {
            return query.QueryString;
        }

        public static implicit operator ResourceQuery(string query)
        {
            return new ResourceQuery(query);
        }
    }
}
