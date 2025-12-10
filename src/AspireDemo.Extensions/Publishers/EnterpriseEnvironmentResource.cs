using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace AspireDemo.Extensions.Publishers;

public class EnterpriseEnvironmentResource
    : Resource, IComputeEnvironmentResource
{
    public EnterpriseEnvironmentResource(string name) : base(name)
    {
        Annotations.Add(new PipelineStepAnnotation(async (factoryContext) =>
        {
            var steps = new List<PipelineStep>();
            steps.Add(new PipelineStep
            {
                Name = $"publish-{Name}",
                Action = PublishAsync,
                Tags = ["publish-environment"],
                RequiredBySteps = [WellKnownPipelineSteps.Publish],
            });

            var resources = factoryContext.PipelineContext.Model.GetComputeResources();
            foreach (var resource in resources)
            {
                var deploymentTarget = resource.GetDeploymentTargetAnnotation(this)?.DeploymentTarget;
                if (deploymentTarget is null)
                {
                    continue;
                }

                if (deploymentTarget.TryGetAnnotationsOfType<PipelineStepAnnotation>(out var annotations))
                {
                    foreach (var annotation in annotations)
                    {
                        var childFactoryContext = new PipelineStepFactoryContext
                        {
                            PipelineContext = factoryContext.PipelineContext,
                            Resource = deploymentTarget
                        };

                        var deploymentTargetSteps = (await annotation.CreateStepsAsync(childFactoryContext)).ToArray();
                        foreach (var step in deploymentTargetSteps)
                        {
                            step.Resource ??= deploymentTarget;
                        }

                        steps.AddRange(deploymentTargetSteps);
                    }
                }
            }

            return steps;
        }));

        Annotations.Add(new PipelineConfigurationAnnotation(context =>
        {
            var resources = context.Model.GetComputeResources().ToList();
            resources.AddRange(context.Model.Resources.OfType<ExternalServiceResource>());

            foreach (var resource in resources)
            {
                var deploymentTarget = resource.GetDeploymentTargetAnnotation(this)?.DeploymentTarget;
                if (deploymentTarget is null)
                {
                    continue;
                }

                var notificationSteps = context.GetSteps(deploymentTarget, "notification");
                var publishStep = context.GetSteps(this, "publish-environment");
                publishStep.DependsOn(notificationSteps);
            }
        }));
    }

    private Task PublishAsync(PipelineStepContext context)
    {
        var contentBuilder = new EmailContentBuilder();

        var recipient = Annotations.OfType<RecipientAnnotation>().FirstOrDefault();
        contentBuilder.AddGreeting(recipient?.Name);

        var environment = context.Model.Resources.OfType<EnterpriseEnvironmentResource>().First();

        var projectResources = environment.ResourceMapping.Values.OfType<EnterpriseProjectResource>().ToArray();
        var containerResources = environment.ResourceMapping.Values.OfType<EnterpriseContainerResource>().ToArray();
        var externalResources = environment.ResourceMapping.Values.OfType<EnterpriseExternalResource>().ToArray();
        
        foreach (var resource in projectResources)
        {
            contentBuilder.AddResource(resource);
        }

        foreach (var resource in containerResources)
        {
            contentBuilder.AddResource(resource);
        }

        contentBuilder.AddExternalResource(externalResources);

        var sender = Annotations.OfType<SenderAnnotation>().FirstOrDefault();
        contentBuilder.AddSignature(sender?.Name);

        var outputService = context.Services.GetRequiredService<IPipelineOutputService>();
        var outputPath = outputService.GetOutputDirectory();
        Directory.CreateDirectory(outputPath);
        var outputFile = Path.Combine(outputPath, "email.txt");

        return File.WriteAllTextAsync(outputFile, contentBuilder.Build(), Encoding.UTF8);
    }

    internal Dictionary<IResource, EnterpriseServiceResource> ResourceMapping { get; } = new();
}