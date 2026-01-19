//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using System;

namespace NosCore.GameObject.InterChannelCommunication.Messages
{
    public class StatData : IMessage
    {
        public Data.WebApi.Character? Character { get; set; }

        public UpdateStatActionType ActionType { get; set; }

        public long Data { get; set; } //TODO: find other type(s)
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
