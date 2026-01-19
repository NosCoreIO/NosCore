//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;
using System;

namespace NosCore.Data.WebApi
{
    public class CharacterRelationStatus
    {
        public Guid CharacterRelationId { get; set; }
        public CharacterRelationType RelationType { get; set; }
        public long CharacterId { get; set; }
        public string? CharacterName { get; set; }
        public bool IsConnected { get; set; }
    }
}
