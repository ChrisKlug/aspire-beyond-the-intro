using Aspire.Hosting;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace AspireDemo.Extensions.ConditionalActivation;

public static class DistributedApplicationExtensions
{
    public static IDistributedApplicationBuilder UseConditionalResourceActivation(
        this IDistributedApplicationBuilder builder,
        params string[] categoriesToActivate)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (categoriesToActivate.Length == 0)
        {
            return builder;
        }

        if (builder.ExecutionContext.IsRunMode)
        {
            if (builder.Services.Any(x => x.ServiceType == typeof(ConditionalResourcesActivationConfiguration)))
            {
                throw new InvalidOperationException(
                    "Conditional resource activation resource has already been added to the application."
                );
            }
            builder.Services.AddSingleton(new ConditionalResourcesActivationConfiguration(categoriesToActivate));
            builder.Services.TryAddEventingSubscriber<ConditionalActivationEventSubscriber>();
        }
        
        return builder;
    }
}