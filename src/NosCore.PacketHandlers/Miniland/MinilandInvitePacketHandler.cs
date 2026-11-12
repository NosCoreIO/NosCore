//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Miniland
{
    public class MinilandInvitePacketHandler(ISessionRegistry sessionRegistry, IMinilandService minilandService, IClock clock)
        : PacketHandler<MinilandInvitePacket>, IWorldPacketHandler
    {

        public override async Task ExecuteAsync(MinilandInvitePacket packet, ClientSession session)
        {
            if (string.IsNullOrWhiteSpace(packet.CharacterName))
            {
                return;
            }

            var character = session.Character;
            var miniland = minilandService.GetMiniland(character.CharacterId);
            if (character.MapInstanceId != miniland.MapInstanceId)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.InvitationOnlyPossibleInMiniland });
                return;
            }

            if (miniland.State == MinilandState.Lock)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.CanNotInviteInLockedMiniland });
                return;
            }

            if (!sessionRegistry.TryGetCharacter(s => s.Name == packet.CharacterName, out var target))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.PlayerIsNotLoggedIn });
                return;
            }

            if (target.CharacterId == character.CharacterId || target.MinilandInviteBlocked)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.PlayerCanNotBeInvited });
                return;
            }

            if (!await character.TryStartInviteAsync(InviteType.Miniland, target.CharacterId, clock))
            {
                return;
            }

            var question = new Dlgi2Packet
            {
                YesPacket = new MJoinPacket { Type = VisualType.Player, VisualId = character.CharacterId },
                NoPacket = new MJoinPacket { Type = VisualType.Npc, VisualId = character.CharacterId },
                Question = Game18NConstString.HasInvitedToMiniland,
                ArgumentType = 1,
                Game18NArguments = { character.Name }
            };
            if (!character.MinilandInviteCharacterIds.TryAdd(target.CharacterId, target.CharacterId))
            {
                await target.SendPacketAsync(question);
                return;
            }

            var seed = character.InventoryService.Values
                .Where(s => s.ItemInstance.Item.Material == MaterialType.SeedOfPower)
                .OrderBy(s => s.Type).ThenBy(s => s.Slot)
                .FirstOrDefault();
            if (seed == null)
            {
                character.MinilandInviteCharacterIds.TryRemove(target.CharacterId, out _);
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotEnoughSeedToInvite });
                return;
            }

            await character.UseUpOneAsync(seed);
            await target.SendPacketAsync(question);
        }
    }
}
