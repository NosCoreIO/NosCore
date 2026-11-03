//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.InterChannelCommunication.Messages
{
    public class FamilyShoutData : IMessage
    {
        public long FamilyId { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
