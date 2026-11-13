//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.LandOfDeathService;
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
    //            and only restores HP/MP to 50%. Not enough seeds =>
    //            fall through to the save-point branch with an info message.
    //   Type 1 — "save point": warp to the respawn stored by IRespawnService,
    //            full HP/MP restore.
    //   Type 2 — "arena" (PVP respawn): −100 gold, full HP/MP, same map, and the arena revival card
    //            ("Invincible in PvP"). Only offered inside an arena.
    //
    // In the Land of Death, type 0 spends a Saver for a full revive on the spot and type 1 goes back
    // to the entrance, which is also where a player without a Saver ends up.
    //
    // Every branch ends with `tp` (position) + `revive` + refreshed `stat`, mirroring
    // the death.txt trace at lines 6338–6344.
    public class RevivalPacketHandler(
        ILogger<RevivalPacketHandler> logger,
        IMapChangeService mapChangeService,
        IRespawnService respawnService,
        IBuffService buffService,
        ICardCatalog cardCatalog,
        IItemCatalog itemCatalog,
        ILandOfDeathService landOfDeathService)
        : PacketHandler<RevivalPacket>, IWorldPacketHandler
    {
        private const short SeedsRequired = 10;
        private const long ArenaReviveCost = 100;

        public override async Task ExecuteAsync(RevivalPacket packet, ClientSession session)
        {
            if (!session.HasPlayerEntity) return;
            var character = session.Character;
            character.IsAlive = true;

            try
            {
                var inLandOfDeath = character.MapInstance?.MapInstanceType == MapInstanceType.LodInstance;
                switch (packet.Type)
                {
                    case RevivalType.InPlace when inLandOfDeath:
                        await ReviveWithSaverAsync(session);
                        break;
                    case RevivalType.AtSavePoint when inLandOfDeath:
                        await landOfDeathService.ReturnToEntranceAsync(session);
                        break;
                    case RevivalType.InPlace when character.MapInstance?.MapInstanceType != MapInstanceType.ArenaInstance:
                        await ReviveInPlaceAsync(session).ConfigureAwait(false);
                        break;
                    case RevivalType.AtSavePoint: await ReviveAtSavePointAsync(session).ConfigureAwait(false); break;
                    case RevivalType.InArena when character.MapInstance?.MapInstanceType == MapInstanceType.ArenaInstance:
                        await ReviveInArenaAsync(session);
                        break;
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
                var seedOfPower = itemCatalog.GetMaterialVNum(MaterialType.SeedOfPower);
                if (character.InventoryService.CountItem(seedOfPower) < SeedsRequired)
                {
                    await session.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.NotEnoughPowerSeed,
                    }).ConfigureAwait(false);
                    await ReviveAtSavePointAsync(session).ConfigureAwait(false);
                    return;
                }

                await session.SendPacketsAsync(
                    character.InventoryService.RemoveItemAmountByVNum(seedOfPower, SeedsRequired));
                percent = 50;

                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.YouHaveUsed,
                    ArgumentType = 2,
                    Game18NArguments = { seedOfPower, SeedsRequired },
                });
            }

            RestoreLife(session, percent);
            await BroadcastReviveAsync(session, character.PositionX, character.PositionY);
        }

        private async Task ReviveWithSaverAsync(ClientSession session)
        {
            var character = session.Character;
            var saver = itemCatalog.GetMaterialVNum(MaterialType.Saver);
            if (character.InventoryService.CountItem(saver) < 1)
            {
                await landOfDeathService.ReturnToEntranceAsync(session);
                return;
            }

            await session.SendPacketsAsync(character.InventoryService.RemoveItemAmountByVNum(saver, 1));
            RestoreLife(session, 100);
            await BroadcastReviveAsync(session, character.PositionX, character.PositionY);
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
            await BroadcastReviveAsync(session, character.PositionX, character.PositionY);

            var protection = cardCatalog.GetTriggeredCard(CardTrigger.ArenaRevival);
            if (protection == null)
            {
                logger.LogWarning("No card is set to protect a fighter revived in the arena");
                return;
            }

            // The client has already been told the fighter is back; losing the protection must not undo that.
            try
            {
                await buffService.ApplyAsync(character, protection,
                    cardCatalog.GetCardBCards(protection.CardId), character);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Arena revival protection failed for character {CharacterId}", character.CharacterId);
            }
        }

        private static void RestoreLife(ClientSession session, int percent)
        {
            var c = session.Character;
            c.Hp = Math.Max(1, c.MaxHp * percent / 100);
            c.Mp = Math.Max(1, c.MaxMp * percent / 100);
            c.IsAlive = true;
        }

        private static async Task BroadcastReviveAsync(ClientSession session, short x, short y)
        {
            var character = session.Character;
            var mapInstance = character.MapInstance;
            if (mapInstance == null) return;

            await character.TeleportAsync(x, y);

            await mapInstance.SendPacketAsync(new RevivePacket
            {
                VisualType = VisualType.Player,
                VisualId = character.VisualId,
                Data = 0,
            }).ConfigureAwait(false);

            await session.SendPacketAsync(character.GeneratePinit()).ConfigureAwait(false);
            await session.SendPacketAsync(character.GenerateStat()).ConfigureAwait(false);
        }
    }
}
