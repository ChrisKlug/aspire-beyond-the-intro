using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using AspireDemo.AppHost.Publishers;

namespace AspireDemo.Extensions.Publishers;

public class EnterpriseEnvironmentResource
    : Resource, IComputeEnvironmentResource
{
    public EnterpriseEnvironmentResource(string name) : base(name)
    {
        Annotations.Add(new PublishingCallbackAnnotation(PublishAsync));
    }

    private async Task PublishAsync(PublishingContext context)
    {
        if (!context.ExecutionContext.IsPublishMode)
        {
            return;
        }

        var projectResources = context.Model.Resources
            .Select(x => x.GetDeploymentTargetAnnotation(this)?.DeploymentTarget)
            .OfType<EnterpriseProjectResource>().ToArray();

        var containerResources = context.Model.Resources
            .Select(x => x.GetDeploymentTargetAnnotation(this)?.DeploymentTarget)
            .OfType<EnterpriseContainerResource>().ToArray();

        var contentBuilder = new EmailContentBuilder();

        await using (var step = await context.ActivityReporter.CreateStepAsync("Compose e-mail"))
        {
            EnterpriseEnvironmentRecipientAnnotation? recipient = null;
            await using (var task = await step.CreateTaskAsync("Get recipient"))
            {
                recipient = Annotations.OfType<EnterpriseEnvironmentRecipientAnnotation>().FirstOrDefault();
                if (recipient == null)
                    await task.SucceedAsync("No recipient added!");
                else
                    await task.SucceedAsync($"Recipient found! ({recipient.Name} <{recipient.Email}>)");
            }

            await using (var task = await step.CreateTaskAsync(
                             "Add friendly greeting. Don't want to upset the recipient..."))
            {
                contentBuilder.AddGreeting(recipient?.Name);
                await task.SucceedAsync("Friendly greeting added!");
            }

            if (projectResources.Any())
            {
                await using (var task = await step.CreateTaskAsync("Adding required IIS apps to e-mail."))
                {
                    foreach (var resource in projectResources)
                    {
                        contentBuilder.AddResource(resource);
                    }

                    await task.SucceedAsync($"{projectResources.Length} apps added.");
                }
            }

            if (containerResources.Any())
            {
                await using (var task = await step.CreateTaskAsync("Adding required containers to e-mail."))
                {
                    foreach (var resource in containerResources)
                    {
                        contentBuilder.AddResource(resource);
                    }

                    await task.SucceedAsync($"{containerResources.Length} containers added.");
                }
            }

            EnterpriseEnvironmentSenderAnnotation? sender = null;
            await using (var task = await step.CreateTaskAsync("Get sender"))
            {
                sender = Annotations.OfType<EnterpriseEnvironmentSenderAnnotation>().FirstOrDefault();
                if (sender == null)
                    await task.SucceedAsync("No sender added!");
                else
                    await task.SucceedAsync($"Sender found! ({sender.Name})");
            }

            await using (var task = await step.CreateTaskAsync("Add signature"))
            {
                contentBuilder.AddSignature(sender?.Name);
                await task.SucceedAsync("Signature added!").ConfigureAwait(false);
            }
        }

        await using (var step = await context.ActivityReporter.CreateStepAsync("\"Send\" e-mail"))
        {
            var filename = Path.Combine(context.OutputPath, "email.txt");
            await File.WriteAllTextAsync(filename, contentBuilder.Build(), Encoding.UTF8);
            await step.SucceedAsync("E-mail content written to " + filename);
        }
    }
}