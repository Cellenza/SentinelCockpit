using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace SentinelCockpit.Engine.Services
{
    public interface IAzureCredentialsProvider
    {
        AzureCredentials Credentials { get; }
    }
}
