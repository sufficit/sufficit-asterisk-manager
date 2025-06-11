using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sufficit.Asterisk.Manager
{
    public static class ObjectBindingExtensions
    {
        private static ILogger _logger = ManagerLogger.CreateLogger(typeof(ObjectBindingExtensions));
        private const char ListSeparator = ',';
        private const char DictionaryPairSeparator = ';';
        private const char DictionaryKeyValueSeparator = '=';

        /// <summary>
        /// Populates the properties of an object from a dictionary of string key-value pairs.
        /// </summary>
        /// <typeparam name="T">The type of the object to bind.</typeparam>
        /// <param name="target">The target object whose properties will be set.</param>
        /// <param name="source">The dictionary containing the values.</param>
        /// <returns>The same target object instance for chaining.</returns>
        public static IDictionary<string, string>? BindFrom<T>(this T target, IDictionary<string, string> source) where T : class
        {
            // pending keys, can't found a property for bind the buffer value
            IDictionary<string, string>? attributes = null;
            if (target == null || source == null)            
                return attributes;            

            var type = target.GetType();

            // Get all public, writable instance properties of the target object
            // and put them in a dictionary for fast, case-insensitive lookups.
            var properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => StripIllegalCharacters(p.Name), p => p, StringComparer.OrdinalIgnoreCase);

            // Iterate over the source dictionary
            foreach (var kvp in source)
            {
                // Try to find a matching property
                if (properties.TryGetValue(kvp.Key, out var propertyInfo))
                {
                    try
                    {
                        // Convert the string value to the property's type
                        var convertedValue = ConvertValue(kvp.Value, propertyInfo.PropertyType);

                        // Set the value on the target object's property
                        propertyInfo.SetValue(target, convertedValue, null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "error on binding object from dictionary, type: {type}, property: {property}", type, propertyInfo.Name);                        
                    }
                } 
                else // appending to pending attributes
                {
                    attributes ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    attributes.Parse(kvp.Key, kvp.Value);
                }
            }

            return attributes;
        }

        /// <summary>
        ///     Strips all illegal charaters from the given lower case string.
        /// </summary>
        /// <param name="s">the original string</param>
        /// <returns>the string with all illegal characters stripped</returns>
        private static string StripIllegalCharacters(string s)
        {
            char c;
            bool needsStrip = false;

            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException(nameof(s));

            for (int i = 0; i < s.Length; i++)
            {
                c = s[i];
                if (c >= '0' && c <= '9')
                    continue;
                if (c >= 'a' && c <= 'z')
                    continue;
                if (c >= 'A' && c <= 'Z')
                    continue;
                needsStrip = true;
                break;
            }

            if (!needsStrip)
                return s;

            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                c = s[i];
                if (c >= '0' && c <= '9')
                    sb.Append(c);
                else if (c >= 'a' && c <= 'z')
                    sb.Append(c);
                else if (c >= 'A' && c <= 'Z')
                    sb.Append(c);
            }

            return sb.ToString();
        }

        private static object? ConvertValue(string value, Type propertyType)
        {
            // If the target type is nullable, get its underlying type.
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (string.IsNullOrEmpty(value))
            {
                // Return default for value types (e.g., 0 for int) or null for reference/nullable types.
                return propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
            }

            // Check if the property is a list type (implements IList)
            if (typeof(IList).IsAssignableFrom(underlyingType))
            {
                // Create an instance of the list (e.g., new List<int>())
                var list = (IList)Activator.CreateInstance(underlyingType)!;
                var itemType = underlyingType.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                // Split the string by the separator
                var items = value.Split(new[] { ListSeparator }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var item in items)
                {
                    // Recursively call ConvertValue for each item in the list
                    list.Add(ConvertValue(item.Trim(), itemType));
                }
                return list;
            }

            // Check if the property is a dictionary type (implements IDictionary)
            if (typeof(IDictionary).IsAssignableFrom(underlyingType))
            {
                var dictionary = (IDictionary)Activator.CreateInstance(underlyingType)!;
                var genericArgs = underlyingType.GetGenericArguments();
                var keyType = genericArgs.Length > 0 ? genericArgs[0] : typeof(object);
                var valueType = genericArgs.Length > 1 ? genericArgs[1] : typeof(object);

                // Split into key-value pairs
                var pairs = value.Split(new[] { DictionaryPairSeparator }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var pair in pairs)
                {
                    var kv = pair.Split(new[] { DictionaryKeyValueSeparator }, 2, StringSplitOptions.None);
                    if (kv.Length == 2)
                    {
                        // Recursively call ConvertValue for the key and the value
                        var key = ConvertValue(kv[0].Trim(), keyType);
                        var val = ConvertValue(kv[1].Trim(), valueType);
                        dictionary.Add(key!, val);
                    }
                }
                return dictionary;
            }

            // Handle Enums
            if (underlyingType.IsEnum)
            {
                return Enum.Parse(underlyingType, value, true);
            }

            // Handle Booleans with support for common string representations
            if (underlyingType == typeof(bool))
            {
                return value.ToLowerInvariant() switch
                {
                    "true" or "t" or "1" or "y" or "yes" or "on" => true,
                    _ => false,
                };
            }

            // Use TypeConverter for robust conversion of other types (int, double, DateTime, etc.)
            var converter = TypeDescriptor.GetConverter(underlyingType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                return converter.ConvertFromString(value);
            }

            // Fallback for other simple types
            return Convert.ChangeType(value, underlyingType);
        }
    }
}