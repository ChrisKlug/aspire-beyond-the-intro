using Aspire.Hosting.ApplicationModel;

namespace AspireDemo.Extensions.Publishers;

public static class EnterpriseEnvironmentExtensions
{
    public static IResourceBuilder<EnterpriseEnvironmentResource> WithSender(
        this IResourceBuilder<EnterpriseEnvironmentResource> builder,
        string name)
    {
        return builder.WithAnnotation(new SenderAnnotation(name));
    }

    public static IResourceBuilder<EnterpriseEnvironmentResource> WithRecipient(
        this IResourceBuilder<EnterpriseEnvironmentResource> builder,
        string name,
        string email)
    {
        return builder.WithAnnotation(new RecipientAnnotation(name, email));
    }
}