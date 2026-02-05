using Aspire.Hosting.ApplicationModel;

namespace AspireDemo.Extensions.ConditionalActivation;

public class ConditionalResourceActivationAnnotation(string category) 
    : IResourceAnnotation
{
    public string Category { get; } = category;
}