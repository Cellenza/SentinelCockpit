using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Newtonsoft.Json.Linq;
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
        public async Task<EventGridEvent> ApplyFirewallOnWebAppsAsync(
            [EventGridTrigger] EventGridEvent eventGridEvent,
            [Blob("/%PROFILE-CONTAINER%/{profile}.json", FileAccess.Read, Connection = "CONNECTIONSTRING_PROFILEBLOBS")] Models.FirewallRules profile)
        {
            if (eventGridEvent == null || profile == null) return null;
            return await ApplyProfileOnAppServiceAsync<IWebApp>(eventGridEvent, profile);
        }

        [FunctionName("ApplyProfileOnFunctionsApps")]
        public async Task<EventGridEvent> ApplyFirewallOnFunctionsAsync(
            [EventGridTrigger] EventGridEvent eventGridEvent,
            [Blob("/%PROFILE-CONTAINER%/{profile}.json", FileAccess.Read, Connection = "CONNECTIONSTRING_PROFILEBLOBS")] Models.FirewallRules profile)
        {
            if (eventGridEvent == null || profile == null) return null;

            return await ApplyProfileOnAppServiceAsync<IFunctionApp>(eventGridEvent, profile);
        }

        private async Task<EventGridEvent> ApplyProfileOnAppServiceAsync<T>(EventGridEvent eventGridEvent, Models.FirewallRules profile)
            where T : IWebAppBase
        {

            Models.Resource appService = ((JObject) eventGridEvent.Data).ToObject<Models.Resource>();
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

            return new EventGridEvent(Guid.NewGuid().ToString(), securedResource.Id, securedResource, "SecuredResource", securedResource.LastUpdate.DateTime, "1.0");
        }
    }
}
