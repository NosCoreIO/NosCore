//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Event;
using System;
using System.Collections.Generic;

namespace NosCore.Core.Configuration
{
    public class ScheduledEventConfiguration
    {
        public EventType Type { get; set; }

        // Times of day, server time, when the event starts; its countdown runs before.
#pragma warning disable CA2227 // Collection properties should be read only
        public List<TimeSpan> At { get; set; } = new();
#pragma warning restore CA2227 // Collection properties should be read only

        // Starts again every Every from midnight, shifted by Offset.
        public TimeSpan? Every { get; set; }

        public TimeSpan Offset { get; set; }
    }
}
