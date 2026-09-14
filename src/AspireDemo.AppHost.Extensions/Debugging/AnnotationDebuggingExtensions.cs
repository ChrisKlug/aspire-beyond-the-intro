using System.Reflection;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AspireDemo.AppHost.Extensions.Debugging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspireDemo.AppHost.Extensions;

public static class AnnotationDebuggingExtensions
{
    public static IResourceBuilder<T> WithAnnotationDebugging<T>(
        this IResourceBuilder<T> builder,
        bool includeProperties = true,
        Func<IResourceAnnotation, bool>? filter = null) where T : IResource
    {
        // BeforeResourceStartedEvent is the last moment before the resource is handed off: every
        // WithXxx() call has run, endpoints are allocated, and the DCP-bound annotations that the
        // orchestrator turns into a custom resource are all in place.
        builder.OnBeforeResourceStarted((resource, @event, _) =>
        {
            var logger = @event.Services.GetRequiredService<ResourceLoggerService>().GetLogger(resource);

            foreach (var line in DescribeAnnotations(resource, includeProperties, filter))
            {
                // Pre-formatted, so pass it as a literal argument to avoid the logger treating
                // braces in a rendered value as message template placeholders.
                logger.LogInformation("{Line}", line);
            }

            return Task.CompletedTask;
        });

        return builder;
    }

    private static IEnumerable<string> DescribeAnnotations(
        IResource resource,
        bool includeProperties,
        Func<IResourceAnnotation, bool>? filter)
    {
        // Snapshot first - the collection is still live while the app is starting up.
        var annotations = resource.Annotations
            .Where(filter ?? (_ => true))
            .OrderBy(a => a.GetType().Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.GetType().FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        yield return "";
        yield return "╔══════════════════════════════════════════════════════════════════════════════";
        yield return $"║ Annotations on '{resource.Name}' ({resource.GetType().Name})";
        yield return $"║ {annotations.Count} annotation(s), {annotations.Select(a => a.GetType()).Distinct().Count()} distinct type(s)";
        yield return "╚══════════════════════════════════════════════════════════════════════════════";

        foreach (var annotation in annotations)
        {
            var type = annotation.GetType();
            var assembly = type.Assembly.GetName().Name ?? "?";

            // Annotations that don't come out of Aspire itself are the interesting ones in the
            // demo - they are the custom ones this app host adds.
            var marker = assembly.StartsWith("Aspire.", StringComparison.Ordinal) ? " " : "*";

            // Blank line per annotation - a wall of properties is unreadable from the back row.
            yield return "";
            yield return $"{marker} {type.Name} ({assembly})";

            if (!includeProperties)
            {
                continue;
            }

            var properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (properties.Count == 0)
            {
                // A marker annotation - it carries no data, its mere presence is the signal.
                yield return "        (no properties - a marker annotation)";
                continue;
            }

            foreach (var property in properties)
            {
                yield return AnnotationFormatter.FormatProperty(property, annotation);
            }
        }

        yield return "";
    }
}
