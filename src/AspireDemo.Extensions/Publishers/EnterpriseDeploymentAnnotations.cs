using Aspire.Hosting.ApplicationModel;

namespace AspireDemo.AppHost.Publishers;

public class EnterpriseEnvironmentRecipientAnnotation(string name, string email) : IResourceAnnotation
{
    public string Name { get; } = name;
    public string Email { get; } = email;
}

public class EnterpriseEnvironmentSenderAnnotation(string name) : IResourceAnnotation
{
    public string Name { get; } = name;
}