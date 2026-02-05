namespace AspireDemo.Extensions.ConditionalActivation;

public class ConditionalResourcesActivationConfiguration(string[] categoriesToActivate)
{
    public string[] CategoriesToActivate { get; } = categoriesToActivate;
}