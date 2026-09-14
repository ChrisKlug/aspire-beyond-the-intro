using System.Diagnostics;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AspireDemo.AppHost.Extensions;

public class PenguinRoutingResource(string name, string workingDirectory)
    : ExecutableResource(name, "ssh", workingDirectory);

public static class PenguinRoutingExtensions
{
    public static IResourceBuilder<T> WithPenguinRouting<T>(
        this IResourceBuilder<T> builder,
        Func<EndpointAnnotation, bool>? endpointFilter = null) where T : IResourceWithEndpoints
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return builder;
        }

        builder.ApplicationBuilder.Services.TryAddEventingSubscriber<PenguinRoutingEventSubscriber>();
        builder.ApplicationBuilder.Services.TryAddSingleton<PenguinTunnelRegistry>();

        // Scan all underlying parent annotations to handle multi-binding deployments automatically
        var parentEndpoints = builder.Resource.Annotations.OfType<EndpointAnnotation>().ToList();
        foreach (var endpoint in parentEndpoints.Where(x => x.UriScheme is "http" or "https")
                                                .Where(endpointFilter ?? (_ => true)))
        {
            var penguinRoutingResource = new PenguinRoutingResource(
                $"{builder.Resource.Name}-pinggy-{endpoint.Name}",
                AppContext.BaseDirectory
            );
            
            // The orchestrator marks an executable Running the instant ssh spawns, which is well
            // before Pinggy has handed out a URL. A health check keeps the resource unhealthy until
            // the tunnel is actually usable - and makes WaitFor on it mean something.
            var healthCheckKey = $"{penguinRoutingResource.Name}-tunnel";
            builder.ApplicationBuilder.Services.AddHealthChecks().Add(new HealthCheckRegistration(
                healthCheckKey,
                sp => new PenguinTunnelHealthCheck(
                    sp.GetRequiredService<PenguinTunnelRegistry>(),
                    penguinRoutingResource.Name
                ),
                failureStatus: null,
                tags: null
            ));

            builder.ApplicationBuilder.AddResource(penguinRoutingResource)
                   .WithArgs(context =>
                   {
                       // Resolved at start-up time, so dynamically allocated ports work too.
                       // TargetPort is where the app itself listens; fall back to the host-facing
                       // port for resources (containers) that only expose a mapped one.
                       var endpointReference = builder.GetEndpoint(endpoint.Name);
                       
                       SetCommandArgs(context, endpointReference);
                   })
                   .WithParentRelationship(builder.Resource)
                   .WaitFor((IResourceBuilder<IResource>)builder)
                   .WithInitialState(new CustomResourceSnapshot
                   {
                       ResourceType = "Penguin Router",
                       State = KnownResourceStates.Starting,
                       Properties = [
                           new("Targeted Local Endpoint", endpoint.Name), 
                           new("Local HTTPS Mode", (endpoint.UriScheme == "https").ToString()
                        )]
                   })
                   .WithHealthCheck(healthCheckKey)
                   .WithoutLifecycleCommands()
                   .WithIconName("VirtualNetwork")
                   .WithCommand(                                                                           
                       name: "open-public-url",                                                                   
                       displayName: "Open public URL",                                                            
                       executeCommand: context =>                                                           
                       {       
                           var registry = context.Services.GetRequiredService<PenguinTunnelRegistry>();
                           if (!registry.TryGetUrl(penguinRoutingResource.Name, out var url))                                    
                               return Task.FromResult(CommandResults.Failure("Tunnel is not up yet"));                             
                                                                                                 
                           Process.Start(new ProcessStartInfo(url!) { UseShellExecute = true });                  
                           return Task.FromResult(CommandResults.Success());                                                       
                       },                                                                                         
                       commandOptions: new CommandOptions                                                         
                       {                                                                                          
                           IsHighlighted = true,                                                                  
                           IconName = "Open",                                                      
                           UpdateState = ctx => ctx.ResourceSnapshot.HealthStatus is HealthStatus.Healthy         
                               ? ResourceCommandState.Enabled                                                     
                               : ResourceCommandState.Disabled                                                    
                       }
                    )
                   .ExcludeFromManifest();
        }

        return builder;
    }

    // This is a HACK! Do not use it! If the name of the annotation, or the API would ever change, it stop working...
    private static IResourceBuilder<T> WithoutLifecycleCommands<T>(this IResourceBuilder<T> builder)
        where T : IResource
    {
        var annotationType = typeof(IResourceAnnotation).Assembly
            .GetType("Aspire.Hosting.ApplicationModel.ExcludeLifecycleCommandsAnnotation");

        if (annotationType is null || Activator.CreateInstance(annotationType) is not IResourceAnnotation annotation)
        {
            return builder;
        }

        builder.Resource.Annotations.Add(annotation);

        return builder;
    }

    private static void SetCommandArgs(CommandLineArgsCallbackContext context, EndpointReference endpoint)
    {
        var localPort = endpoint.TargetPort ?? endpoint.Port;
        
        context.Args.Add("-T"); // no PTY: keeps Pinggy's output plain text so we can parse it
        context.Args.Add("-o"); context.Args.Add("StrictHostKeyChecking=accept-new");
        context.Args.Add("-o"); context.Args.Add("ExitOnForwardFailure=yes");
        context.Args.Add("-o"); context.Args.Add("ServerAliveInterval=30");
        context.Args.Add("-p"); context.Args.Add("443");
        context.Args.Add($"-R0:localhost:{localPort}");
        context.Args.Add("a.pinggy.io");

        if (endpoint.Scheme == "https")
        {
            // Pinggy takes its options as the SSH *remote command*, so this must not be
            // combined with -N. The suffix is the SNI/Host name used against the local
            // server - "localhost" is what the ASP.NET dev certificate is issued for.
            context.Args.Add("x:localServerTls:localhost");
        }
    }
}