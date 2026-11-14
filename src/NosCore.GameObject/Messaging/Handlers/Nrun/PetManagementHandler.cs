//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class PetManagementHandler(IMinilandService minilandService, IClock clock,
            IPartnerSpecialistService partnerSpecialistService, IPetSkillBarService petSkillBarService) : INrunEventHandler
    {
        private const int BarDuration = 3000;

        public NrunRunnerType Runner => NrunRunnerType.PetManagement;

        public bool RequiresNpc => false;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            var character = session.Character;
            if (packet.VisualId is not { } transportId || !character.Mates.TryGetValue(transportId, out var mate))
            {
                return;
            }

            var inOwnMiniland = minilandService.GetMinilandFromMapInstanceId(character.MapInstance.MapInstanceId)
                ?.OwnerId == character.CharacterId;

            switch ((PetManagementAction?)packet.Type)
            {
                case PetManagementAction.JoinTeam:
                    if (!await CheckLevelAsync(character, mate))
                    {
                        return;
                    }

                    var current = character.Mates.Values.FirstOrDefault(s =>
                        s.IsTeamMember && s.MateType == mate.MateType && s != mate);
                    if (current != null)
                    {
                        await StayAsync(character, current, inOwnMiniland);
                    }

                    await JoinAsync(character, mate);
                    break;

                case PetManagementAction.StayInMiniland when inOwnMiniland:
                case PetManagementAction.SendBack when inOwnMiniland:
                    await StayAsync(character, mate, true);
                    break;

                case PetManagementAction.SendBack:
                    await character.SendPacketAsync(new QnaiPacket
                    {
                        YesPacket = Follow(PetManagementAction.ConfirmSendBack, mate),
                        Question = Game18NConstString.AskMateReturn
                    });
                    return;

                case PetManagementAction.ConfirmSendBack:
                    await character.StartDelayAsync(new DelayPacket
                    {
                        Type = DelayPacketType.BackToMiniland,
                        Delay = BarDuration,
                        Packet = Follow(PetManagementAction.SentBack, mate)
                    }, (PetManagementAction.SentBack, mate.MateTransportId), clock.GetCurrentInstant());
                    return;

                case PetManagementAction.SentBack:
                    if (inOwnMiniland || !mate.IsTeamMember
                        || !character.TryCompleteDelay(DelayPacketType.BackToMiniland, (PetManagementAction.SentBack, mate.MateTransportId), clock.GetCurrentInstant()))
                    {
                        return;
                    }

                    await StayAsync(character, mate, false);
                    await character.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.SentBackToMiniland,
                        8, mate.VNum));
                    await character.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.SentBackToMiniland,
                        ArgumentType = 8,
                        Game18NArguments = { mate.VNum }
                    });
                    break;

                case PetManagementAction.Summon:
                    if (character.Mates.Values.Any(s => s.IsTeamMember && s.MateType == mate.MateType))
                    {
                        await character.SendPacketAsync(new MsgiPacket
                        {
                            Type = MessageType.Default,
                            Message = mate.MateType == MateType.Pet
                                ? Game18NConstString.PetAlreadyAccompanying
                                : Game18NConstString.PartnerAlreadyAccompanying
                        });
                        return;
                    }

                    if (!await CheckLevelAsync(character, mate))
                    {
                        return;
                    }

                    await character.StartDelayAsync(new DelayPacket
                    {
                        Type = DelayPacketType.BackToMiniland,
                        Delay = BarDuration,
                        Packet = Follow(PetManagementAction.Summoned, mate)
                    }, (PetManagementAction.Summoned, mate.MateTransportId), clock.GetCurrentInstant());
                    return;

                case PetManagementAction.Summoned:
                    if (mate.IsTeamMember
                        || character.Mates.Values.Any(s => s.IsTeamMember && s.MateType == mate.MateType)
                        || !character.TryCompleteDelay(DelayPacketType.BackToMiniland, (PetManagementAction.Summoned, mate.MateTransportId), clock.GetCurrentInstant())
                        || !await CheckLevelAsync(character, mate))
                    {
                        return;
                    }

                    await JoinAsync(character, mate);
                    await character.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.MateSummoned
                    });
                    break;

                default:
                    return;
            }

            await character.SendPacketAsync(character.GeneratePinit());
            await character.SendPacketsAsync(character.Mates.Values
                .Where(s => s.IsTeamMember)
                .Select(s => s.GeneratePst()));
            await petSkillBarService.RefreshAsync(character);
        }

        private static async Task<bool> CheckLevelAsync(PlayerComponentBundle character, Services.MateService.Mate mate)
        {
            if (character.Level >= mate.Level)
            {
                return true;
            }

            await character.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.CantSetNosmateWithHigherLevel
            });
            return false;
        }

        private static async Task JoinAsync(PlayerComponentBundle character, Services.MateService.Mate mate)
        {
            mate.IsTeamMember = true;
            if (mate.Entity != null)
            {
                return;
            }

            var mapInstance = character.MapInstance;
            var (_, x, y) = MatePlacement.Arrange(character.PositionX, character.PositionY, mapInstance.Map, new[] { mate })[0];
            await mate.SpawnAsync(character, mapInstance, x, y);
            await character.SendPacketAsync(mate.GenerateCond());
        }

        // A mate that stays behind keeps the square it stood on as its Miniland spot; one leaving
        // from elsewhere disappears and is placed when the owner next enters the Miniland.
        private async Task StayAsync(PlayerComponentBundle character, Services.MateService.Mate mate, bool inOwnMiniland)
        {
            await partnerSpecialistService.UntransformAsync(character, mate);
            mate.IsTeamMember = false;
            if (inOwnMiniland)
            {
                if (mate.Entity is { } entity)
                {
                    mate.MapX = entity.PositionX;
                    mate.MapY = entity.PositionY;
                }

                return;
            }

            await mate.DespawnAsync(character.MapInstance);
        }

        private static NrunPacket Follow(PetManagementAction action, Services.MateService.Mate mate) => new()
        {
            Runner = NrunRunnerType.PetManagement,
            Type = (short)action,
            VisualType = VisualType.Monster,
            VisualId = mate.MateTransportId
        };

        private enum PetManagementAction : short
        {
            JoinTeam = 2,
            StayInMiniland = 3,
            SendBack = 4,
            ConfirmSendBack = 5,
            SentBack = 6,
            Summon = 7,
            Summoned = 9
        }
    }
}
