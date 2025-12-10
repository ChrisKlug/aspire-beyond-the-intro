using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.Logging;

namespace AspireDemo.Extensions.Publishers;

public abstract class EnterpriseServiceResource
    : Resource, IResourceWithParent<EnterpriseEnvironmentResource>
{
    protected EnterpriseServiceResource(string name, IResource resource,
        EnterpriseEnvironmentResource enterpriseEnvironmentResource) : base(name)
    {
        TargetResource = resource;
        Parent = enterpriseEnvironmentResource;
        Annotations.Add(new PipelineStepAnnotation(_ =>
        [
            new PipelineStep
            {
                Name = $"{TargetResource.Name}-notification",
                Action = ctx => NotifyAdded(ctx, Parent),
                Tags = ["notification"],
            }
        ]));
    }

    private Task NotifyAdded(PipelineStepContext context, EnterpriseEnvironmentResource environment)
    {
        context.ReportingStep.Log(
            LogLevel.Information,
            $"**{TargetResource.Name}** has been added to **{environment.Name}**.",
            enableMarkdown: true);

        return Task.CompletedTask;
    }

    public EnterpriseEnvironmentResource Parent { get; }
    public IResource TargetResource { get; }
}

public class EnterpriseProjectResource(
    string name,
    IResource resource,
    EnterpriseEnvironmentResource enterpriseEnvironmentResource)
    : EnterpriseServiceResource(name, resource, enterpriseEnvironmentResource);

public class EnterpriseExternalResource(
    string name,
    IResource resource,
    EnterpriseEnvironmentResource enterpriseEnvironmentResource)
    : EnterpriseServiceResource(name, resource, enterpriseEnvironmentResource)
{
    public Uri Uri => ((ExternalServiceResource)TargetResource).Uri!;
}

public class EnterpriseContainerResource(
    string name,
    IResource resource,
    EnterpriseEnvironmentResource enterpriseEnvironmentResource)
    : EnterpriseServiceResource(name, resource, enterpriseEnvironmentResource)
{
    public (string Name,
        string Source,
        string Target,
        ContainerMountType MountType,
        bool ReadOnly)[] GetVolumes()
    {
        if (!TargetResource.TryGetContainerMounts(out var mounts))
        {
            return [];
        }

        return mounts.Select(x => (x.Source!, x.Source!, x.Target, x.Type, x.IsReadOnly)).ToArray();
    }

    public (string Scheme, int ExposedPort, int? InternalPort)[] GetPorts()
    {
        if (!TargetResource.TryGetEndpoints(out var endpoints))
        {
            return [];
        }

        return endpoints.Select(x => (x.UriScheme, x.Port ?? 80, x.TargetPort)).ToArray();
    }
}