//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using System;

namespace NosCore.GameObject.InterChannelCommunication.Messages
{
    // No shutdown time cancels the one a channel is counting down to.
    public class MaintenanceScheduleData : IMessage
    {
        public Instant? ShutdownAt { get; set; }

        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
