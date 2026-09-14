using System.Collections;
using System.Reflection;
using System.Text;
using Aspire.Hosting.ApplicationModel;

namespace AspireDemo.AppHost.Extensions.Debugging;

/// <summary>
/// Turns arbitrary annotation property values into a single readable line - the reflection-driven
/// side of <see cref="AnnotationDebuggingExtensions"/>.
/// </summary>
internal static class AnnotationFormatter
{
    private const int MaxDepth = 1;
    private const int MaxCollectionItems = 10;
    private const int MaxStringLength = 200;
    private const int NameColumnWidth = 34;

    /// <summary>
    /// Formats one property of an annotation as a single indented "name : value" line.
    /// </summary>
    public static string FormatProperty(PropertyInfo property, object instance)
        => $"        {property.Name,-NameColumnWidth} : {ReadAndFormat(property, instance)}";

    /// <summary>
    /// Reads a property off an instance and formats it, turning any getter that throws into a
    /// note rather than taking down the whole dump.
    /// </summary>
    public static string ReadAndFormat(PropertyInfo property, object instance, int depth = 0)
    {
        try
        {
            return Format(property.GetValue(instance), depth);
        }
        catch (Exception ex)
        {
            return $"<threw {(ex.InnerException ?? ex).GetType().Name}>";
        }
    }

    private static string Format(object? value, int depth)
    {
        // Backstop against annotations that hold cyclic or arbitrarily nested object graphs.
        if (depth > MaxDepth + 1)
        {
            return "…";
        }

        switch (value)
        {
            case null:
                return "<null>";
            case string text:
                return Quote(text);
            // Delegates are the whole point of the demo: an annotation isn't just data, it often
            // carries the callback that some later stage of the pipeline invokes.
            case Delegate callback:
                return FormatDelegate(callback);
            case IResource resource:
                return $"{resource.Name} ({resource.GetType().Name})";
            case Type type:
                return type.Name;
            case IEnumerable enumerable when value is not IDictionary:
                return FormatEnumerable(enumerable, depth);
        }

        var valueType = value.GetType();
        if (valueType.IsPrimitive || valueType.IsEnum || HasCustomToString(valueType))
        {
            return Quote(value.ToString() ?? "", quotes: false);
        }

        return depth < MaxDepth ? FormatObject(value, depth) : $"<{valueType.Name}>";
    }

    private static string FormatDelegate(Delegate callback)
    {
        var method = callback.Method;

        // Lambdas live on compiler-generated closure classes ("<>c", "<>c__DisplayClass12_0"),
        // which say nothing useful. Walk out to the type the developer actually wrote.
        var declaringType = method.DeclaringType;
        while (declaringType is { } current && current.Name.StartsWith("<>", StringComparison.Ordinal))
        {
            declaringType = current.DeclaringType;
        }

        // ...and "<WithArgs>b__0" is a lambda declared inside WithArgs.
        var name = method.Name;
        var isLambda = name.StartsWith('<');
        if (isLambda)
        {
            var end = name.IndexOf('>');
            name = end > 1 ? name[1..end].Trim('<', '>', '$') : name;
        }

        if (name == ".ctor")
        {
            name = "ctor";
        }

        var owner = declaringType is null ? "" : declaringType.Name + ".";

        return $"→ {owner}{name}(){(isLambda ? " [lambda]" : "")}";
    }

    private static string FormatEnumerable(IEnumerable enumerable, int depth)
    {
        var items = new List<string>();

        foreach (var item in enumerable)
        {
            if (items.Count == MaxCollectionItems)
            {
                items.Add("…");
                break;
            }

            items.Add(Format(item, depth + 1));
        }

        return items.Count == 0 ? "[]" : $"[{string.Join(", ", items)}]";
    }

    private static string FormatObject(object value, int depth)
    {
        var builder = new StringBuilder("{ ");
        var properties = value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var first = true;
        foreach (var property in properties)
        {
            if (!first)
            {
                builder.Append(", ");
            }

            builder.Append(property.Name).Append(" = ").Append(ReadAndFormat(property, value, depth + 1));
            first = false;
        }

        return first ? $"<{value.GetType().Name}>" : builder.Append(" }").ToString();
    }

    private static bool HasCustomToString(Type type)
        => type.GetMethod(nameof(ToString), BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes)
               ?.DeclaringType != typeof(object);

    private static string Quote(string text, bool quotes = true)
    {
        var single = text.ReplaceLineEndings("↵");
        if (single.Length > MaxStringLength)
        {
            single = single[..MaxStringLength] + "…";
        }

        return quotes ? $"\"{single}\"" : single;
    }
}
