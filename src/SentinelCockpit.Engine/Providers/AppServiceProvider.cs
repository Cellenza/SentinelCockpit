using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.WebJobs;
using SentinelCockpit.Engine.Models;
using SentinelCockpit.Engine.Services;

namespace SentinelCockpit.Engine.Providers
{
    public class AppServiceProvider : BaseAuthenticatedFunctions
    {
        public AppServiceProvider(IAzureCredentialsProvider credentialsProvider) : base(credentialsProvider)
        {
        }

        [FunctionName("ApplyProfileOnWebApps")]
        [return: ServiceBus("%HISTORY-QUEUE%", Connection = "CONNECTIONSTRING_SERVICEBUS")]
        public async Task<Models.Resource> ApplyFirewallOnWebAppsAsync(
            [ServiceBusTrigger("%EVENTS-TOPIC%", "webapps", Connection = "CONNECTIONSTRING_SERVICEBUS")] Models.Resource appService,
            [Blob("/%PROFILE-CONTAINER%/{profile}.json", FileAccess.Read, Connection = "CONNECTIONSTRING_PROFILEBLOBS")] Models.FirewallRules profile)
        {
            if (appService == null || profile == null) return null;

            return await ApplyProfileOnAppServiceAsync<IWebApp>(appService, profile);
        }

        [FunctionName("ApplyProfileOnFunctionsApps")]
        [return: ServiceBus("%HISTORY-QUEUE%", Connection = "CONNECTIONSTRING_SERVICEBUS")]
        public async Task<Models.Resource> ApplyFirewallOnFunctionsAsync(
            [ServiceBusTrigger("%EVENTS-TOPIC%", "functions", Connection = "CONNECTIONSTRING_SERVICEBUS")] Models.Resource appService,
            [Blob("/%PROFILE-CONTAINER%/{profile}.json", FileAccess.Read, Connection = "CONNECTIONSTRING_PROFILEBLOBS")] Models.FirewallRules profile)
        {
            if (appService == null || profile == null) return null;

            return await ApplyProfileOnAppServiceAsync<IFunctionApp>(appService, profile);
        }

        private async Task<Models.Resource> ApplyProfileOnAppServiceAsync<T>(Models.Resource appService, Models.FirewallRules profile)
            where T : IWebAppBase
        {
            var client = AppServiceManager.Authenticate(this.credentialsProvider.Credentials, appService.SubscriptionId);
            bool isFunction = typeof(T) == typeof(IFunctionApp);
            T app;
            if (isFunction)
            {
                app = (T)await client.FunctionApps.GetByIdAsync(appService.Id);
            }
            else
            {
                app = (T)await client.WebApps.GetByIdAsync(appService.Id);
            }

            app.Inner.SiteConfig.IpSecurityRestrictions.Clear();
            foreach (IpRule inboundRule in profile.Inbound)
            {
                foreach (string ipMask in inboundRule.IpMasks)
                {
                    IPNetwork ip = IPNetwork.Parse(ipMask);
                    IpSecurityRestriction rule = new IpSecurityRestriction(ip.Network.ToString(), ip.Netmask.ToString());
                    app.Inner.SiteConfig.IpSecurityRestrictions.Add(rule);
                }
            }

            if (isFunction)
            {
                ((IFunctionApp)app).Update();
            }
            else
            {
                ((IWebApp)app).Update();
            }

            var securedResource = new SecuredResource(appService)
            {
                LastUpdate = DateTime.UtcNow,
                ProfileVersion = profile.Version
            };

            return securedResource;
        }
    }
}
