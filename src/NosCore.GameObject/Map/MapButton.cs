//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Map
{
    // The client draws a button as a map object and asks for it with `get`, so its id stays below
    // the first dropped item's.
    public class MapButton
    {
        public const int LastId = 99999;

        public int MapButtonId { get; init; }

        public short PositionX { get; init; }

        public short PositionY { get; init; }

        public short OffVNum { get; init; }

        public short OnVNum { get; init; }

        public bool Toggles { get; init; }

        public bool IsOn { get; set; }

        public InPacket GenerateIn()
        {
            return new InPacket
            {
                VisualType = VisualType.Object,
                VisualId = MapButtonId,
                VNum = (IsOn ? OnVNum : OffVNum).ToString(),
                PositionX = PositionX,
                PositionY = PositionY,
                InItemSubPacket = new InItemSubPacket
                {
                    Amount = 1
                }
            };
        }

        public OutPacket GenerateOut()
        {
            return new OutPacket
            {
                VisualType = VisualType.Object,
                VisualId = MapButtonId
            };
        }
    }
}
