//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ItemInfoService;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Inventory
{
    public class SpTransformPacketHandler(ISpecialistTransformationService specialistTransformationService,
            IItemInfoService itemInfoService, IBattleStatsProvider battleStatsProvider)
        : PacketHandler<SpTransformPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(SpTransformPacket spTransformPacket, ClientSession clientSession)
        {
            return spTransformPacket.Type == SlPacketType.ChangePoints
                ? SpendPointsAsync(spTransformPacket, clientSession)
                : ToggleAsync(spTransformPacket, clientSession);
        }

        private async Task ToggleAsync(SpTransformPacket packet, ClientSession clientSession)
        {
            var result = await specialistTransformationService.ToggleAsync(clientSession,
                packet.Type == SlPacketType.WearSpAndTransform);
            var msgi = result switch
            {
                SpecialistOnCooldown(var secondsLeft) => new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CantTrasformWithSideEffect,
                    ArgumentType = 4,
                    Game18NArguments = { secondsLeft }
                },
                SpecialistTransformationOutcome.NoSpecialistCardEquipped => Refusal(Game18NConstString.NoSpecialistCardEquipped),
                SpecialistTransformationOutcome.InVehicle => Refusal(Game18NConstString.CantUseInVehicle),
                SpecialistTransformationOutcome.NoPoints => Refusal(Game18NConstString.ZeroPointsTransformationCancelled),
                _ => null,
            };
            if (msgi == null)
            {
                return;
            }

            await clientSession.SendPacketAsync(msgi);
        }

        private static MsgiPacket Refusal(Game18NConstString message) =>
            new() { Type = MessageType.Default, Message = message };

        private async Task SpendPointsAsync(SpTransformPacket packet, ClientSession clientSession)
        {
            var result = await specialistTransformationService.SpendPointsAsync(clientSession, packet.TransportId,
                packet.SpecialistDamage ?? 0, packet.SpecialistDefense ?? 0, packet.SpecialistElement ?? 0,
                packet.SpecialistHP ?? 0);
            switch (result)
            {
                case SpecialistPointsOutcome.CardNotWorn:
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.SpecialistCardMustBeEquipped
                    });
                    break;
                case SpecialistPointsSpent(var card):
                    await clientSession.SendPacketAsync(itemInfoService.GenerateInfo(card));
                    await clientSession.SendPacketAsync(clientSession.Character.GenerateSc(battleStatsProvider));
                    break;
            }
        }
    }
}
