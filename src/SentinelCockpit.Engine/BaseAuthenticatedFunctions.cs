using SentinelCockpit.Engine.Services;

namespace SentinelCockpit.Engine
{
    public abstract class BaseAuthenticatedFunctions
    {
        protected readonly IAzureCredentialsProvider credentialsProvider;

        public BaseAuthenticatedFunctions(IAzureCredentialsProvider credentialsProvider)
        {
            this.credentialsProvider = credentialsProvider;
        }
    }
}
