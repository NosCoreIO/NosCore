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
            var message = result.Outcome switch
            {
                SpecialistTransformationOutcome.NoSpecialistCardEquipped => Game18NConstString.NoSpecialistCardEquipped,
                SpecialistTransformationOutcome.InVehicle => Game18NConstString.CantUseInVehicle,
                SpecialistTransformationOutcome.NoPoints => Game18NConstString.ZeroPointsTransformationCancelled,
                SpecialistTransformationOutcome.OnCooldown => Game18NConstString.CantTrasformWithSideEffect,
                _ => (Game18NConstString?)null,
            };
            if (message == null)
            {
                return;
            }

            var msgi = new MsgiPacket
            {
                Type = MessageType.Default,
                Message = message.Value
            };
            if (result.Outcome == SpecialistTransformationOutcome.OnCooldown)
            {
                msgi.ArgumentType = 4;
                msgi.Game18NArguments.Add(result.CooldownSecondsLeft);
            }

            await clientSession.SendPacketAsync(msgi);
        }

        private async Task SpendPointsAsync(SpTransformPacket packet, ClientSession clientSession)
        {
            var result = await specialistTransformationService.SpendPointsAsync(clientSession, packet.TransportId,
                packet.SpecialistDamage ?? 0, packet.SpecialistDefense ?? 0, packet.SpecialistElement ?? 0,
                packet.SpecialistHP ?? 0);
            switch (result.Outcome)
            {
                case SpecialistPointsOutcome.CardNotWorn:
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.SpecialistCardMustBeEquipped
                    });
                    break;
                case SpecialistPointsOutcome.Spent when result.Card is { } card:
                    await clientSession.SendPacketAsync(itemInfoService.GenerateInfo(card));
                    await clientSession.SendPacketAsync(clientSession.Character.GenerateSc(battleStatsProvider));
                    break;
            }
        }
    }
}
