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

using System.Collections.Generic;

namespace Eterra.Common
{
    /// <summary>
    /// Specifies flags for the available character sets (unicode blocks) which
    /// should be included in a <see cref="Graphics.SpriteFont"/> instance.
    /// Supports bitwise combination of different elements 
    /// (e.g. <c><see cref="CharSet.BasicLatin"/> | 
    /// <see cref="CharSet.Latin1Supplement"/></c>).
    /// </summary>
    public enum CharSet
    {
        /// <summary>
        /// Contains all letters of the ASCII encoding. Control codes are
        /// omitted. Ranges from U+0020 to U+007E and defines 94 characters.
        /// </summary>
        BasicLatin = 1,
        /// <summary>
        /// Contains letters for French, German, Icelandic and Spanish
        /// (Latin-1 punctuation including Umlauts, symbols, 30 pairs
        /// of majuscule and minuscule accented Latin characters) and symbol
        /// sets for punctuation, mathematics and currencies. 
        /// Control characters are omitted. 
        /// Ranges from U+00A1 to U+00FF and defines 94 characters.
        /// </summary>
        Latin1Supplement = 2,
        /// <summary>
        /// Contains letters for Afrikaans, Catalan, Czech, Esperanto, 
        /// Hungarian, Latin, Latvian, Lithuanian, Maltese, Northern Sami, 
        /// Polish, Serbo-Croatian, Slovak, Slovene, Sorbian, Turkish and 
        /// Welsh. Ranges from U+0100 to U+017F and defines 127 characters.
        /// </summary>
        LatinExtendedA = 4,
        /// <summary>
        /// Contains letters for African alphabet, Americanist, Khoisan, 
        /// Pan-Nigerian, Pinyin and Romanian. 
        /// Ranges from U+0180 to U+024F and defines 207 characters.
        /// </summary>
        LatinExtendedB = 8,
        /// <summary>
        /// Contains letters for Greek and Coptic.
        /// Ranges from U+0370 to U+03FF and defines 143 characters.
        /// </summary>
        GreekAndCoptic = 16,
        /// <summary>
        /// Contains letters for Russian, Ukrainian, Belarusian, Bulgarian, 
        /// Serbian, Macedonian and Abkhaz.
        /// Ranges from U+0400 to U+04FF and defines 255 characters.
        /// </summary>
        Cyrillic = 32,
        /// <summary>
        /// Contains letters for Abkhaz, Komi, Mordvin and Aleut.
        /// Ranges from U+0500 to U+052F and defines 47 characters.
        /// </summary>
        CyrillicSupplement = 64,
        /// <summary>
        /// Contains the letters of <see cref="BasicLatin"/>,
        /// <see cref="Latin1Supplement"/> and <see cref="LatinExtendedA"/>,
        /// which should cover most of the use-cases for applications targeting
        /// Central Europe, Northern Europe, Western Europe, Southern Europe,
        /// and America. Defines 315 characters.
        /// </summary>
        DefaultEuropeAmerica = BasicLatin | Latin1Supplement | LatinExtendedA,
        /// <summary>
        /// Contains the letters of <see cref="BasicLatin"/>, which should 
        /// cover use-cases which only use the basic ASCII characters.
        /// </summary>
        DefaultASCII = BasicLatin
    }

    /// <summary>
    /// Provides extension methods for using the <see cref="CharSet"/> enum.
    /// </summary>
    public static class CharSetExtension
    {
        private static readonly Dictionary<CharSet, char[]> charSets
            = new Dictionary<CharSet, char[]>();

        static CharSetExtension()
        {
            charSets[CharSet.BasicLatin] = CreateRange(0x0020, 0x007E);
            charSets[CharSet.Latin1Supplement] = CreateRange(0x00A1, 0x00FF);
            charSets[CharSet.LatinExtendedA] = CreateRange(0x0100, 0x017F);
            charSets[CharSet.LatinExtendedB] = CreateRange(0x0180, 0x024F);
            charSets[CharSet.GreekAndCoptic] = CreateRange(0x0370, 0x03FF);
            charSets[CharSet.Cyrillic] = CreateRange(0x0400, 0x04FF);
            charSets[CharSet.CyrillicSupplement] = CreateRange(0x0500, 0x052F);
        }

        /// <summary>
        /// Gets an array with every <see cref="char"/> in a 
        /// <see cref="CharSet"/>.
        /// </summary>
        /// <param name="charSet">
        /// The <see cref="CharSet"/> which should be converted to a 
        /// <see cref="char"/> array. Supports bitwise combination of different
        /// elements (e.g. <c><see cref="CharSet.BasicLatin"/> | 
        /// <see cref="CharSet.Latin1Supplement"/></c>).
        /// </param>
        /// <returns>
        /// A new <see cref="char"/> array instance.
        /// </returns>
        public static char[] GetCharacters(this CharSet charSet)
        {
            char[] output = new char[CountCharacters(charSet)];
            int outputIterator = 0;

            foreach (CharSet charSetFlag in charSets.Keys)
            {
                if (charSet.HasFlag(charSetFlag))
                {
                    char[] charSetCharacters = charSets[charSetFlag];
                    for (int i = 0; i < charSetCharacters.Length; i++)
                        output[outputIterator++] = charSetCharacters[i];
                }
            }

            return output;
        }

        /// <summary>
        /// Counts the amount of characters in a <see cref="CharSet"/>.
        /// </summary>
        /// <param name="charSet">
        /// The <see cref="CharSet"/> which should be converted to a 
        /// <see cref="char"/> array. Supports bitwise combination of different
        /// elements (e.g. <c><see cref="CharSet.BasicLatin"/> | 
        /// <see cref="CharSet.Latin1Supplement"/></c>).
        /// </param>
        /// <returns>
        /// The amount of characters of the specified 
        /// <paramref name="charSet"/> as int greater than/equal to 0.
        /// </returns>
        public static int CountCharacters(this CharSet charSet)
        {
            int charCount = 0;
            foreach (CharSet charSetFlag in charSets.Keys)
                if (charSet.HasFlag(charSetFlag))
                    charCount += charSets[charSetFlag].Length;
            return charCount;
        }

        private static char[] CreateRange(int firstCharCode, int lastCharCode)
        {
            char[] charRange = new char[1 + lastCharCode - firstCharCode];

            for (int i = 0; i < charRange.Length; i++)
                charRange[i] = (char)(i + firstCharCode);

            return charRange;
        }
    }
}
