using System;
using Newtonsoft.Json;

namespace SentinelCockpit.Engine.Models
{
    [JsonObject]
    public class SecuredResource : Resource
    {
        [JsonProperty("profileVersion", Required = Required.Always)]
        public string ProfileVersion { get; set; }

        [JsonProperty("lastUpdate", Required = Required.Always)]
        public DateTimeOffset LastUpdate { get; set; }
     
        public SecuredResource() { }

        public SecuredResource(Resource resource)
        {
            this.Id = resource.Id;
            this.Name = resource.Name;
            this.SubscriptionId = resource.SubscriptionId;
            this.ResourceGroup = resource.ResourceGroup;
            this.Provider = resource.Provider;
            this.Type = resource.Type;
            this.Profile = resource.Profile;
        }
    }
}
