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

using System;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Xunit;

namespace SentinelCockpit.Engine.UnitTests
{
    public class EngineFunctionsUnitTests
    {
        [Fact]
        public async Task EngineFunctions_ResourceCreatedAsync_EventTypeNotEqualToResourceWriteSuccessEvent_ShouldReturnNull()
        {
            EngineFunctions engine = new EngineFunctions(null);
            EventGridEvent eventGridEvent = new EventGridEvent(string.Empty, string.Empty, null, EventTypes.ContainerRegistryImageDeletedEvent, DateTime.Now, "2.0", "topic");

            EventGridEvent message = await engine.ResourceCreatedAsync(eventGridEvent, null);
            Assert.Null(message);
        }
    }
}
