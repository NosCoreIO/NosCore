//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ItemInfoService;
using NosCore.GameObject.Services.SpecialistCardService;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Inventory
{
    public class SpTransformPacketHandler(IClock clock, ITransformationService transformationService,
            ISpecialistCardService specialistCardService, IItemInfoService itemInfoService,
            IBattleStatsProvider battleStatsProvider, IVitalityService vitalityService)
        : PacketHandler<SpTransformPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SpTransformPacket spTransformPacket, ClientSession clientSession)
        {
            if (spTransformPacket.Type == SlPacketType.ChangePoints)
            {
                await SpendPointsAsync(spTransformPacket, clientSession);
            }
            else
            {
                if (clientSession.Character.IsSitting)
                {
                    return;
                }

                if (!(clientSession.Character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance is SpecialistInstance specialistInstance))
                {
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.NoSpecialistCardEquipped
                    });
                    return;
                }

                if (clientSession.Character.IsVehicled)
                {
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.CantUseInVehicle
                    });
                    return;
                }

                var currentRunningSeconds = (clock.GetCurrentInstant() - clientSession.Character.LastSp).TotalSeconds;

                if (clientSession.Character.UseSp)
                {
                    clientSession.Character.LastSp = clock.GetCurrentInstant();
                    await transformationService.RemoveSpAsync(clientSession);
                }
                else
                {
                    if ((clientSession.Character.SpPoint == 0) && (clientSession.Character.SpAdditionPoint == 0))
                    {
                        await clientSession.SendPacketAsync(new MsgiPacket
                        {
                            Type = MessageType.Default,
                            Message = Game18NConstString.ZeroPointsTransformationCancelled
                        });
                        return;
                    }

                    if (currentRunningSeconds >= clientSession.Character.SpCooldown)
                    {
                        if (spTransformPacket.Type == SlPacketType.WearSpAndTransform)
                        {
                            if (clientSession.Character.TryCompleteDelay(DelayPacketType.Locomotion, specialistInstance.Id, clock.GetCurrentInstant()))
                            {
                                await transformationService.ChangeSpAsync(clientSession);
                            }
                        }
                        else
                        {
                            await clientSession.Character.StartDelayAsync(new DelayPacket
                            {
                                Type = DelayPacketType.Locomotion,
                                Delay = 5000,
                                Packet = new SpTransformPacket { Type = SlPacketType.WearSpAndTransform }
                            }, specialistInstance.Id, clock.GetCurrentInstant());
                            await clientSession.Character.MapInstance.SendPacketAsync(new GuriPacket
                            {
                                Type = GuriPacketType.Dance,
                                Argument = 1,
                                EntityId = clientSession.Character.CharacterId
                            });
                        }
                    }
                    else
                    {
                        await clientSession.SendPacketAsync(new MsgiPacket
                        {
                            Type = MessageType.Default,
                            Message = Game18NConstString.CantTrasformWithSideEffect,
                            ArgumentType = 4,
                            Game18NArguments = { (short)(clientSession.Character.SpCooldown - (int)Math.Round(currentRunningSeconds)) }
                        });
                    }
                }
            }
        }
        private async Task SpendPointsAsync(SpTransformPacket packet, ClientSession clientSession)
        {
            var character = clientSession.Character;
            if (!character.UseSp
                || character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance
                    is not SpecialistInstance specialist
                || packet.TransportId != specialist.TransportId)
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.SpecialistCardMustBeEquipped
                });
                return;
            }

            if (!specialistCardService.TrySpendPoints(specialist, packet.SpecialistDamage ?? 0,
                    packet.SpecialistDefense ?? 0, packet.SpecialistElement ?? 0, packet.SpecialistHP ?? 0))
            {
                return;
            }

            await vitalityService.RefreshAndNotifyAsync(character);
            await clientSession.SendPacketAsync(itemInfoService.GenerateInfo(specialist)!);
            await clientSession.SendPacketAsync(character.GenerateSc(battleStatsProvider));
        }
    }
}
