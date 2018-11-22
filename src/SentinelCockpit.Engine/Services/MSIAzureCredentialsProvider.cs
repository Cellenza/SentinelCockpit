using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace SentinelCockpit.Engine.Services
{
    public class MSIAzureCredentialsProvider : IAzureCredentialsProvider
    {
        public MSIAzureCredentialsProvider()
        {
            var msiInfo = new MSILoginInformation(MSIResourceType.AppService);
            this.Credentials = SdkContext.AzureCredentialsFactory.FromMSI(msiInfo, AzureEnvironment.AzureGlobalCloud);
        }

        public AzureCredentials Credentials { get; private set; }
    }
}
