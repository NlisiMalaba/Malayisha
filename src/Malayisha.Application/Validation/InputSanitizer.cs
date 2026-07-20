using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Malayisha.Application.Validation;

public static partial class InputSanitizer
{
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var withoutNulls = trimmed.Replace("\0", string.Empty, StringComparison.Ordinal);
        var withoutControl = new string(
            withoutNulls.Where(static c => !char.IsControl(c) || c is '\t' or '\n' or '\r').ToArray());

        var withoutScriptBlocks = ScriptBlockPattern().Replace(withoutControl, string.Empty);
        return HtmlTagPattern().Replace(withoutScriptBlocks, string.Empty);
    }

    public static void SanitizeInstance(object? instance)
    {
        if (instance is null)
        {
            return;
        }

        SanitizeObject(instance, new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    private static void SanitizeObject(object instance, HashSet<object> visited)
    {
        if (!visited.Add(instance))
        {
            return;
        }

        var type = instance.GetType();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (property.PropertyType == typeof(string))
            {
                var current = (string?)property.GetValue(instance);
                var sanitized = Sanitize(current);

                if (!string.Equals(current, sanitized, StringComparison.Ordinal))
                {
                    SetPropertyValue(instance, property, sanitized);
                }

                continue;
            }

            if (IsStringCollection(property.PropertyType))
            {
                SanitizeStringCollection(instance, property);
                continue;
            }

            var value = property.GetValue(instance);
            if (value is null || IsSimpleType(property.PropertyType))
            {
                continue;
            }

            if (value is IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is not null && !IsSimpleType(item.GetType()))
                    {
                        SanitizeObject(item, visited);
                    }
                }

                continue;
            }

            SanitizeObject(value, visited);
        }
    }

    private static void SanitizeStringCollection(object instance, PropertyInfo property)
    {
        if (property.GetValue(instance) is not IEnumerable items)
        {
            return;
        }

        var sanitizedItems = items
            .Cast<object?>()
            .Select(static item => item is string text ? Sanitize(text) : item)
            .ToArray();

        if (sanitizedItems.All(static item => item is string))
        {
            var sanitizedStrings = sanitizedItems.Cast<string>().ToArray();
            var originalStrings = items.Cast<object?>().Select(static item => item as string).ToArray();

            if (sanitizedStrings.SequenceEqual(originalStrings, StringComparer.Ordinal))
            {
                return;
            }

            if (property.PropertyType.IsArray)
            {
                SetPropertyValue(instance, property, sanitizedStrings);
                return;
            }

            if (typeof(IList).IsAssignableFrom(property.PropertyType)
                && property.GetValue(instance) is IList mutableList)
            {
                for (var index = 0; index < mutableList.Count; index++)
                {
                    mutableList[index] = sanitizedStrings[index];
                }

                return;
            }

            SetPropertyValue(instance, property, sanitizedStrings);
        }
    }

    private static void SetPropertyValue(object instance, PropertyInfo property, object? value)
    {
        if (property.SetMethod is { IsPublic: true })
        {
            property.SetValue(instance, value);
            return;
        }

        var declaringType = instance.GetType();
        var positionalBackingField = declaringType.GetField(
            $"<{property.Name}>i__Field",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (positionalBackingField is not null)
        {
            positionalBackingField.SetValue(instance, value);
            return;
        }

        var autoPropertyBackingField = declaringType.GetField(
            $"<{property.Name}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        autoPropertyBackingField?.SetValue(instance, value);
    }

    private static bool IsStringCollection(Type propertyType)
    {
        if (propertyType == typeof(string))
        {
            return false;
        }

        if (!typeof(IEnumerable).IsAssignableFrom(propertyType))
        {
            return false;
        }

        if (propertyType.IsGenericType)
        {
            var elementType = propertyType.GetGenericArguments()[0];
            return elementType == typeof(string);
        }

        return propertyType.IsArray && propertyType.GetElementType() == typeof(string);
    }

    private static bool IsSimpleType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(TimeSpan)
            || underlying == typeof(Guid);
    }

    [GeneratedRegex(@"<\s*script\b[^>]*>.*?</\s*script\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex ScriptBlockPattern();

    [GeneratedRegex("<[^>]*>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagPattern();
}
