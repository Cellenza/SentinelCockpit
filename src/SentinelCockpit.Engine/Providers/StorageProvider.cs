﻿//  Copyright 2018 Cellenza
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

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Newtonsoft.Json.Linq;
using SentinelCockpit.Engine.Models;
using SentinelCockpit.Engine.Services;
using Action = Microsoft.Azure.Management.Storage.Fluent.Models.Action;

namespace SentinelCockpit.Engine.Providers
{
    public class StorageFirewallRules : BaseAuthenticatedFunctions
    {
        public StorageFirewallRules(IAzureCredentialsProvider credentialsProvider) : base(credentialsProvider)
        {
        }

        [FunctionName("ApplyProfileOnStorage")]
        public async Task<EventGridEvent> ApplyFirewallOnStorageAsync(
            [EventGridTrigger] EventGridEvent eventGridEvent,
            [Blob("/%PROFILE-CONTAINER%/{profile}.json", FileAccess.Read, Connection = "CONNECTIONSTRING_PROFILEBLOBS")] Models.FirewallRules profile)
        {
            if (eventGridEvent == null || profile == null) return null;

            var storage = ((JObject) eventGridEvent.Data).ToObject<Models.Resource>();
            var client = StorageManager.Authenticate(this.credentialsProvider.Credentials, storage.SubscriptionId);
            var storageAccount = await client.StorageAccounts.GetByIdAsync(storage.Id);

            storageAccount.Inner.NetworkRuleSet.DefaultAction = Enum.GetName(typeof(DefaultAction), DefaultAction.Deny);
            storageAccount.Inner.NetworkRuleSet.IpRules.Clear();
            foreach (IpRule inboundRule in profile.Inbound)
            {
                foreach (string ipMask in inboundRule.IpMasks)
                {
                    var action = inboundRule.Action == Models.Action.Allow ? Action.Allow : (Action?)null;
                    var rule = new IPRule(ipMask, action);
                    storageAccount.Inner.NetworkRuleSet.IpRules.Add(rule);
                }
            }

            storageAccount.Update();

            var securedResource = new SecuredResource(storage)
            {
                LastUpdate = DateTime.UtcNow,
                ProfileVersion = profile.Version
            };

            return new EventGridEvent(Guid.NewGuid().ToString(), securedResource.Id, securedResource, "SecuredResource", securedResource.LastUpdate.DateTime, "1.0");
        }
    }
}
