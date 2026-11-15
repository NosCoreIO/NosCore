//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.Parser.Parsers
{
    // packet.txt joins captures from several sessions, and the game numbers its NPCs and monsters
    // afresh in each, so one NPC shows up under a new id per session, and an id of one session can
    // belong to another NPC in the next. Ids of the same kind of NPC or monster first seen on the same
    // cell of the same map are one entity.
    public static class CaptureSessions
    {
        private const long UnknownNpc = -1;

        private readonly record struct Sighting(VisualType Type, short Map, string? VNum, long Id);

        // The NPCs the capture shows, whose shops, teleports and recipes the capture owns.
        public static HashSet<int> NpcIds(IEnumerable<IPacket> packets) => packets.OfType<InPacket>()
            .Where(s => s.VisualType == VisualType.Npc).Select(s => (int)s.VisualId).ToHashSet();

        public static void MergeRepeatedEntities(List<IPacket> packets)
        {
            var sightings = new Dictionary<InPacket, Sighting>(ReferenceEqualityComparer.Instance);
            var firstCells = new Dictionary<Sighting, (short X, short Y)>();
            short map = 0;
            foreach (var packet in packets)
            {
                switch (packet)
                {
                    case AtPacket at:
                        map = at.MapId;
                        break;
                    case InPacket { VisualType: VisualType.Npc or VisualType.Monster } entity:
                        var sighting = new Sighting(entity.VisualType, map, entity.VNum, entity.VisualId);
                        sightings[entity] = sighting;
                        firstCells.TryAdd(sighting, (entity.PositionX, entity.PositionY));
                        break;
                }
            }

            var entities = firstCells.GroupBy(c => (c.Key.Type, c.Key.Map, c.Key.VNum, c.Value.X, c.Value.Y), c => c.Key).ToList();
            var demand = entities.SelectMany(e => e.Select(s => (s.Type, s.Id))).CountBy(s => s).ToDictionary();
            var used = firstCells.Keys.Select(s => (s.Type, s.Id)).ToHashSet();
            var ids = new Dictionary<Sighting, long>();
            var taken = new HashSet<(VisualType Type, long Id)>();
            var dropped = new HashSet<Sighting>();
            long nextId = 1;
            foreach (var entity in entities)
            {
                var type = entity.Key.Type;
                foreach (var sighting in entity)
                {
                    demand[(type, sighting.Id)]--;
                }

                // An id another entity can still want goes to it, and an NPC left without a free id gets
                // a new one. A monster left without one is nearly always one already kept, met again in
                // another session after it walked, so it is not imported twice.
                long? id = entity.Select(e => e.Id).Where(c => !taken.Contains((type, c)))
                    .OrderBy(c => demand[(type, c)]).Select(c => (long?)c).FirstOrDefault();
                if (id == null && type == VisualType.Npc)
                {
                    while (used.Contains((type, nextId)))
                    {
                        nextId++;
                    }

                    id = nextId;
                    used.Add((type, nextId));
                }

                if (id is not { } kept)
                {
                    dropped.UnionWith(entity);
                    continue;
                }

                taken.Add((type, kept));
                foreach (var sighting in entity)
                {
                    ids[sighting] = kept;
                }
            }

            // A packet about an NPC can come before the capture shows it, so it falls back to the first
            // entity seen under that id on the map.
            var currentIds = new Dictionary<(VisualType Type, short Map, long Id), long>();
            foreach (var sighting in firstCells.Keys.Reverse().Where(ids.ContainsKey))
            {
                currentIds[(sighting.Type, sighting.Map, sighting.Id)] = ids[sighting];
            }

            long Resolve(VisualType type, long id) =>
                currentIds.TryGetValue((type, map, id), out var merged) ? merged : type == VisualType.Npc ? UnknownNpc : id;

            map = 0;
            foreach (var packet in packets)
            {
                switch (packet)
                {
                    case AtPacket at:
                        map = at.MapId;
                        break;
                    case InPacket entity when sightings.TryGetValue(entity, out var sighting) && ids.TryGetValue(sighting, out var kept):
                        currentIds[(entity.VisualType, map, entity.VisualId)] = kept;
                        entity.VisualId = kept;
                        break;
                    case EffectPacket effect:
                        effect.VisualEntityId = Resolve(effect.EffectType, effect.VisualEntityId);
                        break;
                    case ShopPacket shop:
                        shop.VisualId = Resolve(shop.VisualType, shop.VisualId);
                        break;
                    case NInvPacket inventory:
                        inventory.VisualId = Resolve(inventory.VisualType, inventory.VisualId);
                        break;
                    case NrunPacket { VisualType: { } type, VisualId: { } id } run:
                        run.VisualId = Resolve(type, id);
                        break;
                }
            }

            packets.RemoveAll(p => p is InPacket entity && sightings.TryGetValue(entity, out var sighting) && dropped.Contains(sighting));
        }
    }
}
