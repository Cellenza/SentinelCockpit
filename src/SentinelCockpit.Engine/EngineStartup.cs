using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SentinelCockpit.Engine.Services;

[assembly:WebJobsStartup(typeof(SentinelCockpit.Engine.EngineStartup))]
namespace SentinelCockpit.Engine
{
    public class EngineStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddSingleton<IAzureCredentialsProvider, MSIAzureCredentialsProvider>();
        }
    }
}
