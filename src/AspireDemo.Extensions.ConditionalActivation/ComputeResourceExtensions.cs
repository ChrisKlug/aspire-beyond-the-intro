using Aspire.Hosting.ApplicationModel;

namespace AspireDemo.Extensions.ConditionalActivation;

public static class ComputeResourceExtensions
{
    public static IResourceBuilder<ProjectResource> WithConditionalActivation(
        this IResourceBuilder<ProjectResource> resource, 
        params string[] categories
    )
    {
        AddConditionalActivationAnnotation(resource.Resource, categories);
        return resource;
    }
    public static IResourceBuilder<ContainerResource> WithConditionalActivation(
        this IResourceBuilder<ContainerResource> resource,  
        params string[] categories
    )
    {
        AddConditionalActivationAnnotation(resource.Resource, categories);
        return resource;
    }
    public static IResourceBuilder<ExecutableResource> WithConditionalActivation(
        this IResourceBuilder<ExecutableResource> resource,  
        params string[] categories
    )
    {
        AddConditionalActivationAnnotation(resource.Resource, categories);
        return resource;
    }
    
    private static void AddConditionalActivationAnnotation(
        this IResource resource, 
        params string[] categories
    )
    {
        foreach (var category in categories)
        {
            resource.Annotations.Add(new ConditionalResourceActivationAnnotation(category));
        }
    }
}