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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace SentinelCockpit.Engine.Models
{
    [JsonObject]
    public class FirewallRules
    {
        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; set; }

        [JsonProperty("inbound", Required = Required.Always)]
        public List<IpRule> Inbound { get; set; }

        [JsonProperty("outbound", Required = Required.Always)]
        public List<IpRule> Outbound { get; set; }
    }
}
