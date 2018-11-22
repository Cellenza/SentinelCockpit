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

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SentinelCockpit.Engine.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Action
    {
        [EnumMember(Value = "Deny")]
        Deny = 0,

        [EnumMember(Value = "Allow")]
        Allow = 1,
    }
}
