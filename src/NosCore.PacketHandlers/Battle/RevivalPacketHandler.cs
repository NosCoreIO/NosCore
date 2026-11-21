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
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.LandOfDeathService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

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
        ILogger<RevivalPacketHandler> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
        IMapChangeService mapChangeService,
        IRespawnService respawnService,
        IBuffService buffService,
        ICardCatalog cardCatalog,
        IItemCatalog itemCatalog,
        ILandOfDeathService landOfDeathService,
        IScriptedInstanceService scriptedInstanceService,
        ILowLevelBuffService lowLevelBuffService)
        : PacketHandler<RevivalPacket>, IWorldPacketHandler
    {
        private const short SeedsRequired = 10;
        private const long ArenaReviveCost = 100;

        public override async Task ExecuteAsync(RevivalPacket packet, ClientSession session)
        {
            if (!session.HasPlayerEntity) return;
            var character = session.Character;
            // Only the dead can be revived: a living player must not spend a Saver to heal or ask to be sent back.
            if (character.IsAlive) return;
            // A scripted run brings back its own players, from the answer to the dialog it asked. The Land of
            // Death is a scripted run too, but its revival (a Saver on the spot or back to the entrance) is below.
            if (character.MapInstance is { } map && map.MapInstanceType != MapInstanceType.LodInstance
                && scriptedInstanceService.GetRun(map.MapInstanceId) is { } run)
            {
                await run.RevivalAnsweredAsync(character);
                return;
            }

            character.IsAlive = true;

            var protect = false;
            try
            {
                var inLandOfDeath = character.MapInstance?.MapInstanceType == MapInstanceType.LodInstance;
                switch (packet.Type)
                {
                    case RevivalType.InPlace when inLandOfDeath:
                        ClaimPendingRevive(character);
                        await ReviveWithSaverAsync(session);
                        break;
                    case RevivalType.AtSavePoint when inLandOfDeath:
                        ClaimPendingRevive(character);
                        await landOfDeathService.ReturnToEntranceAsync(session);
                        break;
                    case RevivalType.InPlace when character.MapInstance?.MapInstanceType != MapInstanceType.ArenaInstance:
                        await ReviveInPlaceAsync(session);
                        break;
                    case RevivalType.AtSavePoint: await ReviveAtSavePointAsync(session); break;
                    case RevivalType.InArena when character.MapInstance?.MapInstanceType == MapInstanceType.ArenaInstance:
                        protect = await ReviveInArenaAsync(session);
                        break;
                    default:
                        character.IsAlive = false;
                        return;
                }
            }
            catch
            {
                character.IsAlive = false;
                throw;
            }

            // Outside the try above: the client has already been told the fighter is back, so a failure
            // here must not undo that.
            if (protect)
            {
                await ApplyArenaProtectionAsync(character);
            }
        }

        // The Land of Death's revive timeout acts only while the ask it made is still pending; answering it
        // before the first await keeps that timeout from also sending the player to the entrance.
        private static void ClaimPendingRevive(PlayerComponentBundle character) => character.ReviveAskedAt = default;

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
                    });
                    await ReviveAtSavePointAsync(session);
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
            var respawnMapTypeId = respawnService.ResolveRevivalMapTypeId(character);
            var (mapId, x, y) = respawnService.GetRespawnLocation(character, respawnMapTypeId);

            RestoreLife(session, 100);
            await mapChangeService.ChangeMapAsync(session, mapId, x, y);
            await BroadcastReviveAsync(session, x, y);
        }

        // False when the fighter could not pay and went back to the save point instead.
        private async Task<bool> ReviveInArenaAsync(ClientSession session)
        {
            var character = session.Character;
            if (character.Gold < ArenaReviveCost)
            {
                await ReviveAtSavePointAsync(session);
                return false;
            }

            character.Gold -= ArenaReviveCost;
            RestoreLife(session, 100);
            await session.SendPacketAsync(character.GenerateGold());
            await BroadcastReviveAsync(session, character.PositionX, character.PositionY);
            return true;
        }

        private async Task ApplyArenaProtectionAsync(PlayerComponentBundle character)
        {
            var protection = cardCatalog.GetTriggeredCard(CardTrigger.ArenaRevival);
            if (protection == null)
            {
                logger.LogWarning(logLanguage[LogLanguageKey.ARENA_REVIVAL_CARD_MISSING]);
                return;
            }

            await buffService.ApplyAsync(character, protection, cardCatalog.GetCardBCards(protection.CardId), character);
        }

        private static void RestoreLife(ClientSession session, int percent)
        {
            var c = session.Character;
            c.Hp = Math.Max(1, c.MaxHp * percent / 100);
            c.Mp = Math.Max(1, c.MaxMp * percent / 100);
            c.IsAlive = true;
        }

        private async Task BroadcastReviveAsync(ClientSession session, short x, short y)
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
            });

            await session.SendPacketAsync(character.GeneratePinit());
            await session.SendPacketAsync(character.GenerateStat());
            await lowLevelBuffService.RefreshAsync(character);
        }
    }
}
