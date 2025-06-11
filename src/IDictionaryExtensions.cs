using System;
using System.Collections.Generic;
using System.Text;
using static Sufficit.Asterisk.Common;

namespace Sufficit.Asterisk.Manager
{
    /// <summary>
    ///     Methods for buffer manipulations
    /// </summary>
    public static class IDictionaryExtensions
    {
        /// <summary>
        /// Parses a line in "Key: Value" format and adds it to the strongly-typed dictionary.
        /// Keys are converted to invariant lower case for consistent lookups.
        /// </summary>
        public static void AddKeyValue(this IDictionary<string, string> source, string line)
        {
            // Use a ReadOnlySpan to avoid allocating new strings during parsing.
            // The conversion is done explicitly with .AsSpan() for .NET Standard 2.0 compatibility.
            ReadOnlySpan<char> lineSpan = line.AsSpan();

            int delimiterIndex = lineSpan.IndexOf(':');

            // The validation logic remains the same.
            if (delimiterIndex <= 0)            
                return;            

            // Slice the span instead of creating substrings. No memory allocation here.
            ReadOnlySpan<char> keySpan = lineSpan.Slice(0, delimiterIndex).Trim();
            ReadOnlySpan<char> valueSpan = lineSpan.Slice(delimiterIndex + 1).Trim();

            // The rest of the code works as expected.
            string key = keySpan.ToString().ToLowerInvariant();
            string value = valueSpan.SequenceEqual("<null>".AsSpan()) ? string.Empty : valueSpan.ToString();

            source.Parse(key, value);           
        }

        /// <summary>
        ///     Adds or updates a key-value pair in the specified dictionary.
        /// </summary>
        /// <remarks>If the <paramref name="key"/> already exists in the dictionary, the <paramref name="value"/> is appended to the existing value, separated by a predefined line separator. If the <paramref name="key"/> does not exist, the key-value pair is added to the dictionary.</remarks>
        /// <param name="source">The dictionary to which the key-value pair will be added or updated. Cannot be null.</param>
        /// <param name="key">The key to add or update in the dictionary. Cannot be null.</param>
        /// <param name="value">The value to associate with the specified key. Cannot be null.</param>
        public static void Parse (this IDictionary<string, string> source, string key, string value)
        {
            if (source.TryGetValue(key, out string? existingVal))
                source[key] = string.Concat(existingVal, LINE_SEPARATOR, value);
            else
                source[key] = value;
        }
    }
}
