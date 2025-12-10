using System.Text;
using Aspire.Hosting.ApplicationModel;

namespace AspireDemo.Extensions.Publishers;

internal class EmailContentBuilder
{
    private StringBuilder contents = new();
    private bool hasAddedProjectResources;
    private bool hasAddedContainerResources;

    public EmailContentBuilder AddGreeting(string? name)
    {
        contents.Append(@$"Dear {name ?? "IT-person"}!
We need to deploy our solution, and because of this, we need some stuff from you...

Here are the specifics...

");
        return this;
    }

    public EmailContentBuilder AddResource(EnterpriseProjectResource resource)
    {
        if (!hasAddedProjectResources)
        {
            contents.AppendLine("We need the following IIS web apps:");
            hasAddedProjectResources = true;
        }

        contents.AppendLine($" - {resource.Name}");
        return this;
    }

    public EmailContentBuilder AddResource(EnterpriseContainerResource resource)
    {
        if (!hasAddedContainerResources)
        {
            contents.AppendLine(
                $"\r\nWe{(hasAddedProjectResources ? " also " : " ")}need the following container(s) to be hosted:");
            hasAddedContainerResources = true;
        }

        ContainerImageAnnotation? image = null;
        if (resource.TargetResource.TryGetAnnotationsOfType<ContainerImageAnnotation>(
                out var imageAnnotations))
        {
            image = imageAnnotations.FirstOrDefault();
        }

        contents.AppendLine($" - {resource.Name} ({image!.Registry}/{image!.Image}:{image!.Tag})");
        foreach (var port in resource.GetPorts())
        {
            contents.AppendLine($"   - {port.Scheme}: {port.ExposedPort}");
        }

        foreach (var mount in resource.GetVolumes())
        {
            contents.AppendLine($"   - Mount: {mount.Target}{(mount.ReadOnly ? " (as read only)" : "")}");
        }

        return this;
    }

    public EmailContentBuilder AddExternalResource(EnterpriseExternalResource[] resources)
    {
        if (resources.Length == 0)
            return this;

        contents.AppendLine(
            $"\r\nWe also need to make sure that the firewall allows us to talk to:");

        foreach (var resource in resources)
            contents.AppendLine($" - {resource.Uri}");

        return this;
    }

    public EmailContentBuilder AddSignature(string? name)
    {
        contents.Append(@$"
Thank you for the help!
// {name ?? "The Development Team"}
");
        return this;
    }

    public string Build() => contents.ToString();
}