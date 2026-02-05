using Aspire.Hosting.ApplicationModel;

namespace AspireDemo.Extensions.Publishing;

public class RecipientAnnotation(string name, string email) : IResourceAnnotation
{
    public string Name { get; } = name;
    public string Email { get; } = email;
}

public class SenderAnnotation(string name) : IResourceAnnotation
{
    public string Name { get; } = name;
}