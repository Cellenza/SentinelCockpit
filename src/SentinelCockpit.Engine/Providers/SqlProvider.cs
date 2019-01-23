using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Management.Sql.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Newtonsoft.Json.Linq;
using SentinelCockpit.Engine.Models;
using SentinelCockpit.Engine.Services;

namespace SentinelCockpit.Engine.Providers
{
    public class SqlProvider : BaseAuthenticatedFunctions
    {
        public SqlProvider(IAzureCredentialsProvider credentialsProvider) : base(credentialsProvider)
        {
        }

        [FunctionName("ApplyProfileOnSqlServers")]
        public async Task<EventGridEvent> ApplyFirewallOnSqlServersAsync(
            [EventGridTrigger] EventGridEvent eventGridEvent,
            [Blob("/%PROFILE-CONTAINER%/{profile}.json", FileAccess.Read, Connection = "CONNECTIONSTRING_PROFILEBLOBS")] Models.FirewallRules profile)
        {
            if (eventGridEvent == null || profile == null) return null;

            var sqlServer = ((JObject)eventGridEvent.Data).ToObject<Models.Resource>();
            var client = SqlManager.Authenticate(this.credentialsProvider.Credentials, sqlServer.SubscriptionId);
            var server = await client.SqlServers.GetByIdAsync(sqlServer.Id);

            foreach (IpRule inboundRule in profile.Inbound)
            {
                var ipMasks = inboundRule.IpMasks;
                for (int i = 0; i < ipMasks.Count; ++i)
                {
                    string ruleName = $"{inboundRule.Name}_{i}";
                    IPNetwork ip = IPNetwork.Parse(ipMasks[i]);
                    var rule = await server.FirewallRules.GetAsync(ruleName);
                    if (rule == null)
                    {
                        await server.FirewallRules.Define(ruleName)
                                                  .WithIPAddressRange(ip.FirstUsable.ToString(), ip.LastUsable.ToString())
                                                  .CreateAsync();
                    }
                    else
                    {
                        rule.Inner.StartIpAddress = ip.FirstUsable.ToString();
                        rule.Inner.EndIpAddress = ip.LastUsable.ToString();
                        rule.Update();
                    }

                }
            }
            
            var securedResource = new SecuredResource(sqlServer)
            {
                LastUpdate = DateTime.UtcNow,
                ProfileVersion = profile.Version
            };

            return new EventGridEvent(Guid.NewGuid().ToString(), securedResource.Id, securedResource, "SecuredResource", securedResource.LastUpdate.DateTime, "1.0");
        }
    }
}
