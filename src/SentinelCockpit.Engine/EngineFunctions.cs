//  Copyright 2018 Cellenza
//  This file is part of SentinelCockpit.

//  SentinelCockpit is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  SentinelCockpit is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with SentinelCockpit. If not, see <https://www.gnu.org/licenses/>.

using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SentinelCockpit.Engine.Models;
using SentinelCockpit.Engine.Services;

namespace SentinelCockpit.Engine
{
    public class EngineFunctions : BaseAuthenticatedFunctions
    {
        private const string ApplyFireWallProfileTag = "Firewall.Profile";

        private const string NoneFirewallProfile = "none";

        private const string DefaultFireWallProfile = "default";

        public EngineFunctions(IAzureCredentialsProvider credentialsProvider) : base(credentialsProvider)
        {
        }

        [FunctionName("ResourceCreated")]
        [return: ServiceBus("%EVENTS-TOPIC%", Connection = "CONNECTIONSTRING_SERVICEBUS", EntityType = EntityType.Topic)]
        public async Task<Message> ResourceCreatedAsync(
            [EventGridTrigger] EventGridEvent eventGridEvent,
            ILogger log)
        {
            if (eventGridEvent == null ||
                eventGridEvent.EventType != EventTypes.ResourceWriteSuccessEvent)
            {
                return null;
            }

            Message message = null;
            string resourceId = eventGridEvent.Subject;
            string subscriptionId = ResourceUtils.SubscriptionFromResourceId(resourceId);
            var azure = ResourceManager.Configure().Authenticate(credentialsProvider.Credentials).WithSubscription(subscriptionId);
            var resource = await azure.GenericResources.GetByIdAsync(resourceId);

            if (!resource.Tags.TryGetValue(ApplyFireWallProfileTag, out string profile))
            {
                profile = DefaultFireWallProfile;
            }

            if (!string.Equals(profile, NoneFirewallProfile))
            {
                var data = new Models.Resource
                {
                    Profile = profile,
                    SubscriptionId = subscriptionId,
                    Id = resourceId,
                    Name = ResourceUtils.NameFromResourceId(resourceId),
                    ResourceGroup = ResourceUtils.GroupFromResourceId(resourceId),
                    Provider = ResourceUtils.ResourceProviderFromResourceId(resourceId),
                    Type = ResourceUtils.ResourceTypeFromResourceId(resourceId)
                };

                string json = JsonConvert.SerializeObject(data, Formatting.None);
                message = new Message(Encoding.UTF8.GetBytes(json));
                message.UserProperties.Add("provider", data.Provider);
                message.UserProperties.Add("type", data.Type);
            }

            return message;
        }


        [FunctionName("StoreHistory")]
        public async Task StoreHistoryAsync(
            [ServiceBusTrigger("%HISTORY-QUEUE%", Connection = "CONNECTIONSTRING_SERVICEBUS")] Models.SecuredResource resource,
            [Table("%HISTORY-TABLE%", Connection = "CONNECTIONSTRING_HISTORYTABLE")] CloudTable historyTable,
            ILogger log)
        {
            if (resource == null)
            {
                return;
            }

            var entity = new TableEntityAdapter<SecuredResource>(resource, resource.SubscriptionId, resource.Id)
            {
                Timestamp = resource.LastUpdate
            };

            TableOperation operation = TableOperation.InsertOrMerge(entity);
            await historyTable.ExecuteAsync(operation);
        }
    }
}
