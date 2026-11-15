//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Portals;
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
        private const int WanderReach = 8;

        private readonly record struct Sighting(VisualType Type, short Map, string? VNum, long Id);

        // The NPCs the capture shows, whose shops, teleports and recipes the capture owns.
        public static HashSet<int> NpcIds(IEnumerable<IPacket> packets) => packets.OfType<InPacket>()
            .Where(s => s.VisualType == VisualType.Npc).Select(s => (int)s.VisualId).ToHashSet();

        // Sources come oldest first. The newest source that shows a map replaces the NPCs, monsters and
        // portals older sources show there, since the game moves and retires them between versions.
        public static List<IPacket> Combine(IReadOnlyList<List<IPacket>> sources)
        {
            var source = new Dictionary<IPacket, int>(ReferenceEqualityComparer.Instance);
            var owners = new Dictionary<short, int>();
            for (var i = 0; i < sources.Count; i++)
            {
                foreach (var packet in sources[i])
                {
                    source[packet] = i;
                    if (packet is AtPacket at)
                    {
                        owners[at.MapId] = i;
                    }
                }
            }

            var packets = sources.SelectMany(s => s).ToList();
            MergeRepeatedEntities(packets);
            var kept = new List<IPacket>(packets.Count);
            short map = 0;
            foreach (var packet in packets)
            {
                if (packet is AtPacket at)
                {
                    map = at.MapId;
                }
                else if (packet is InPacket { VisualType: VisualType.Npc or VisualType.Monster } or GpPacket
                    && owners.TryGetValue(map, out var owner) && source[packet] != owner)
                {
                    continue;
                }

                kept.Add(packet);
            }

            return kept;
        }

        // The maps the capture shows, whose NPCs, monsters and portals it owns.
        public static HashSet<short> Maps(IEnumerable<IPacket> packets) => packets.OfType<AtPacket>().Select(s => s.MapId).ToHashSet();

        public static void MergeRepeatedEntities(List<IPacket> packets)
        {
            var sightings = new Dictionary<InPacket, Sighting>(ReferenceEqualityComparer.Instance);
            var firstCells = new Dictionary<Sighting, (short X, short Y)>();
            var visits = new Dictionary<Sighting, HashSet<int>>();
            short map = 0;
            var visit = 0;
            foreach (var packet in packets)
            {
                switch (packet)
                {
                    case AtPacket at:
                        map = at.MapId;
                        visit++;
                        break;
                    case InPacket { VisualType: VisualType.Npc or VisualType.Monster } entity:
                        var sighting = new Sighting(entity.VisualType, map, entity.VNum, entity.VisualId);
                        sightings[entity] = sighting;
                        firstCells.TryAdd(sighting, (entity.PositionX, entity.PositionY));
                        (visits.TryGetValue(sighting, out var seen) ? seen : visits[sighting] = []).Add(visit);
                        break;
                }
            }

            var entities = firstCells.GroupBy(c => (c.Key.Type, c.Key.Map, c.Key.VNum, c.Value.X, c.Value.Y), c => c.Key).ToList();
            var demand = entities.SelectMany(e => e.Select(s => (s.Type, s.Id))).CountBy(s => s).ToDictionary();
            var used = firstCells.Keys.Select(s => (s.Type, s.Id)).ToHashSet();
            var ids = new Dictionary<Sighting, long>();
            var taken = new HashSet<(VisualType Type, long Id)>();
            var dropped = new HashSet<Sighting>();
            var keptMonsters = new Dictionary<(short Map, string? VNum), List<(short X, short Y, HashSet<int> Visits)>>();
            long nextId = 1;
            foreach (var entity in entities)
            {
                var type = entity.Key.Type;
                foreach (var sighting in entity)
                {
                    demand[(type, sighting.Id)]--;
                }

                // An id another entity can still want goes to it. An entity left without a free id gets a
                // new one, unless it is a monster close to a kept one of its kind that is never on screen
                // with it: that is the same monster met again on another visit after it walked.
                long? id = entity.Select(e => e.Id).Where(c => !taken.Contains((type, c)))
                    .OrderBy(c => demand[(type, c)]).Select(c => (long?)c).FirstOrDefault();
                var cell = (entity.Key.Map, entity.Key.VNum);
                var entityVisits = entity.SelectMany(s => visits[s]).ToHashSet();
                if (id == null && (type == VisualType.Npc || !keptMonsters.TryGetValue(cell, out var near)
                    || !near.Any(k => Math.Abs(k.X - entity.Key.X) + Math.Abs(k.Y - entity.Key.Y) <= WanderReach
                        && !k.Visits.Overlaps(entityVisits))))
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
                if (type == VisualType.Monster)
                {
                    (keptMonsters.TryGetValue(cell, out var list) ? list : keptMonsters[cell] = []).Add((entity.Key.X, entity.Key.Y, entityVisits));
                }

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
