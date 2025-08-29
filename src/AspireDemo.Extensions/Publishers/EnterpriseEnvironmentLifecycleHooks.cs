using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using AspireDemo.AppHost.Publishers;

namespace AspireDemo.Extensions.Publishers;

public class EnterpriseEnvironmentLifecycleHooks(DistributedApplicationExecutionContext executionContext)
    : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (executionContext.IsRunMode)
        {
            return Task.CompletedTask;
        }

        var enterpriseEnvironment = appModel.Resources.OfType<EnterpriseEnvironmentResource>().First();

        foreach (var r in appModel.GetComputeResources())
        {
            EnterpriseServiceResource serviceResource = r switch
            {
                ProjectResource pr => new EnterpriseProjectResource(r.Name, pr, enterpriseEnvironment),
                ContainerResource cr => new EnterpriseContainerResource(r.Name, cr, enterpriseEnvironment),
                _ => throw new NotSupportedException($"Resource {r.Name} is not supported.")
            };

            r.Annotations.Add(new DeploymentTargetAnnotation(serviceResource)
            {
                ComputeEnvironment = enterpriseEnvironment
            });
        }

        return Task.CompletedTask;
    }
}