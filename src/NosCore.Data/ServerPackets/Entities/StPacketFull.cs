//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Packets;
using NosCore.Packets.Attributes;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.ServerPackets.Entities
{
    // Observed wire from the official server (main-city trace, NPC 3019 at full health):
    //   st 2 3019 35 0 100 100 1360 630 1360 630 0
    //   └─ type  id  lvl heroLvl hp% mp% currHp currMp maxHp maxMp buffs
    //
    // NosCore.Packets v16.2's StPacket stops at `CurrentMp` (idx 7) + `BuffIds` (idx 8) —
    // missing MaxHp/MaxMp, which is why the target info card renders 100/100 after a hit
    // (the client reads max from st, not from e_info, after the initial selection). This
    // local override carries the full field set. Serializer is keyed by Type.Name, so
    // StPacketFull coexists with the nuget's StPacket without colliding; the "st" header
    // is server-outbound only so no deserializer collision either.
    [PacketHeader("st", Scope.InGame)]
    public class StPacketFull : PacketBase
    {
        [PacketIndex(0)]
        public VisualType Type { get; set; }

        [PacketIndex(1)]
        public long VisualId { get; set; }

        [PacketIndex(2)]
        public byte Level { get; set; }

        [PacketIndex(3)]
        public byte HeroLvl { get; set; }

        [PacketIndex(4)]
        public int HpPercentage { get; set; }

        [PacketIndex(5)]
        public int MpPercentage { get; set; }

        [PacketIndex(6)]
        public int CurrentHp { get; set; }

        [PacketIndex(7)]
        public int CurrentMp { get; set; }

        [PacketIndex(8)]
        public int MaxHp { get; set; }

        [PacketIndex(9)]
        public int MaxMp { get; set; }

        [PacketIndex(10, SpecialSeparator = " ", IsOptional = true)]
        public List<short>? BuffIds { get; set; }
    }
}
