//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;

namespace NosCore.GameObject.InterChannelCommunication.Messages
{
    public class FamilyChangedData : IMessage
    {
        public long FamilyId { get; set; }

        /// <summary>The characters who joined or left; everyone else in the family only needs a refresh.</summary>
        public List<long> CharacterIds { get; set; } = new();

        public string? JoinedCharacterName { get; set; }

        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
