//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.InterChannelCommunication.Messages
{
    public class DisconnectSessionData : IMessage
    {
        public long SessionId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
