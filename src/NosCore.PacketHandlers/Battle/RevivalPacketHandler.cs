//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Microsoft.Extensions.Logging;

namespace NosCore.PacketHandlers.Battle
{
    // Client-driven revive branch after the death dlgi. Three flavours:
    //   Type 0 — "continue here": free at lvl<=20; lvl>20 costs 10x Seed of Power
    //            (item 1012) and only restores HP/MP to 50%. Not enough seeds =>
    //            fall through to the save-point branch with an info message.
    //   Type 1 — "save point": warp to the respawn stored by IRespawnService,
    //            full HP/MP restore.
    //   Type 2 — "arena" (PVP respawn): −100 gold, full HP/MP, same map.
    //
    // Every branch ends with `tp` (position) + `revive` + refreshed `stat`, mirroring
    // the death.txt trace at lines 6338–6344.
    public class RevivalPacketHandler(
        ILogger<RevivalPacketHandler> logger,
        IMapChangeService mapChangeService,
        IRespawnService respawnService)
        : PacketHandler<RevivalPacket>, IWorldPacketHandler
    {
        private const short SeedOfPowerVNum = 1012;
        private const short SeedsRequired = 5;
        private const long ArenaReviveCost = 100;

        public override async Task ExecuteAsync(RevivalPacket packet, ClientSession session)
        {
            if (!session.HasPlayerEntity) return;
            var character = session.Character;
            if (character.IsAlive) return;
            character.IsAlive = true;

            try
            {
                switch (packet.Type)
                {
                    case 0: await ReviveInPlaceAsync(session).ConfigureAwait(false); break;
                    case 1: await ReviveAtSavePointAsync(session).ConfigureAwait(false); break;
                    case 2: await ReviveInArenaAsync(session).ConfigureAwait(false); break;
                    default:
                        character.IsAlive = false;
                        return;
                }
            }
            catch (Exception ex)
            {
                character.IsAlive = false;
                logger.LogWarning(ex, "Revival failed for character {CharacterId} type {Type}",
                    character.CharacterId, packet.Type);
            }
        }

        private async Task ReviveInPlaceAsync(ClientSession session)
        {
            var character = session.Character;
            var percent = 100;

            if (character.Level > 20)
            {
                if (character.InventoryService.CountItem(SeedOfPowerVNum) < SeedsRequired)
                {
                    await session.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.NotEnoughPowerSeed,
                    }).ConfigureAwait(false);
                    await ReviveAtSavePointAsync(session).ConfigureAwait(false);
                    return;
                }

                ConsumeSeeds(session, SeedsRequired);
                percent = 50;

                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.FivePowerSeedUsed,
                }).ConfigureAwait(false);
            }

            RestoreLife(session, percent);
            await BroadcastReviveAsync(session, character.MapX, character.MapY).ConfigureAwait(false);
        }

        private async Task ReviveAtSavePointAsync(ClientSession session)
        {
            var character = session.Character;
            var respawnMapTypeId = respawnService.ResolveRespawnMapTypeId(character.MapId);
            var (mapId, x, y) = respawnService.GetRespawnLocation(character, respawnMapTypeId);

            RestoreLife(session, 100);
            await mapChangeService.ChangeMapAsync(session, mapId, x, y).ConfigureAwait(false);
            await BroadcastReviveAsync(session, x, y).ConfigureAwait(false);
        }

        private async Task ReviveInArenaAsync(ClientSession session)
        {
            var character = session.Character;
            if (character.Gold < ArenaReviveCost) { await ReviveAtSavePointAsync(session).ConfigureAwait(false); return; }

            character.Gold -= ArenaReviveCost;
            RestoreLife(session, 100);
            await session.SendPacketAsync(character.GenerateGold()).ConfigureAwait(false);
            await BroadcastReviveAsync(session, character.MapX, character.MapY).ConfigureAwait(false);
        }

        private static void RestoreLife(ClientSession session, int percent)
        {
            var c = session.Character;
            c.Hp = Math.Max(1, c.MaxHp * percent / 100);
            c.Mp = Math.Max(1, c.MaxMp * percent / 100);
            c.IsAlive = true;
        }

        private static void ConsumeSeeds(ClientSession session, short needed)
        {
            var inventory = session.Character.InventoryService;
            short remaining = needed;
            foreach (var slot in inventory.GetAll().Where(i => i.ItemInstance?.ItemVNum == SeedOfPowerVNum).ToList())
            {
                if (remaining <= 0) break;
                var take = (short)Math.Min(remaining, slot.ItemInstance!.Amount);
                inventory.RemoveItemAmountFromInventory(take, slot.ItemInstanceId);
                remaining -= take;
            }
        }

        private static async Task BroadcastReviveAsync(ClientSession session, short x, short y)
        {
            var character = session.Character;
            var mapInstance = character.MapInstance;
            if (mapInstance == null) return;

            await mapInstance.SendPacketAsync(new TpPacket
            {
                VisualType = VisualType.Player,
                VisualId = character.VisualId,
                X = x,
                Y = y,
            }).ConfigureAwait(false);

            await mapInstance.SendPacketAsync(new RevivePacket
            {
                VisualType = VisualType.Player,
                VisualId = character.VisualId,
                Data = 0,
            }).ConfigureAwait(false);

            await session.SendPacketAsync(character.Group.GeneratePinit()).ConfigureAwait(false);
            await session.SendPacketAsync(character.GenerateStat()).ConfigureAwait(false);
        }
    }
}
