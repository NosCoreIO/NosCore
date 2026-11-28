//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // On arrival the at packet names the grid the client loads and the c_map after it names the map the
    // player is on. They differ where the server shows one grid under several ids, each its own map:
    // the soulstone rooms (at 2107, c_map 2112 for the Grey Soulstone's room) and Sunny Meadows' hidden
    // chicken place (at 2104, c_map 2110). What the capture sees there belongs to the shown map.
    public static class ShownMaps
    {
        // Moves map along the capture; true when the packet was an arrival.
        public static bool Follow(IPacket packet, ref short map)
        {
            switch (packet)
            {
                case AtPacket at:
                    map = at.MapId;
                    return true;
                case CMapPacket { IsEntering: true } shown:
                    map = shown.Id;
                    return true;
                default:
                    return false;
            }
        }

        // Every map shown under an id other than its grid's, with the at packet that loaded its grid.
        public static Dictionary<short, AtPacket> Grids(IEnumerable<IPacket> packets)
        {
            var grids = new Dictionary<short, AtPacket>();
            AtPacket? arrival = null;
            foreach (var packet in packets)
            {
                switch (packet)
                {
                    case AtPacket at:
                        arrival = at;
                        break;
                    case CMapPacket { IsEntering: true } shown when arrival is { } loaded:
                        if (shown.Id != loaded.MapId)
                        {
                            grids.TryAdd(shown.Id, loaded);
                        }

                        arrival = null;
                        break;
                }
            }

            return grids;
        }
    }
}
