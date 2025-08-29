using Aspire.Hosting.ApplicationModel;
using AspireDemo.Extensions.Publishers;

namespace AspireDemo.AppHost.Publishers;

public abstract class EnterpriseServiceResource(
    string name,
    IResource resource,
    EnterpriseEnvironmentResource composeEnvironmentResource)
    : Resource(name), IResourceWithParent<EnterpriseEnvironmentResource>
{
    public IResource TargetResource { get; } = resource;

    public EnterpriseEnvironmentResource Parent => composeEnvironmentResource;
}

public class EnterpriseProjectResource(
    string name,
    ProjectResource resource,
    EnterpriseEnvironmentResource composeEnvironmentResource)
    : EnterpriseServiceResource(name, resource, composeEnvironmentResource)
{
}

public class EnterpriseContainerResource(
    string name,
    ContainerResource resource,
    EnterpriseEnvironmentResource composeEnvironmentResource)
    : EnterpriseServiceResource(name, resource, composeEnvironmentResource)
{
    public int[] GetPorts()
    {
        if (TargetResource.TryGetEndpoints(out var endpoints))
        {
            return endpoints
                .Where(x => x.TargetPort.HasValue)
                .Select(x => x.TargetPort!.Value).ToArray();
        }

        return [];
    }

    public ContainerMountAnnotation[] GetMounts()
    {
        if (TargetResource.TryGetContainerMounts(out var mounts))
        {
            return mounts.ToArray();
        }

        return [];
    }
}