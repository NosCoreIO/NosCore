//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Threading.Tasks;


//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.PacketHandlers.Inventory
{
    public class SpTransformPacketHandler(IClock clock, ITransformationService transformationService,
            IGameLanguageLocalizer gameLanguageLocalizer)
        : PacketHandler<SpTransformPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SpTransformPacket spTransformPacket, ClientSession clientSession)
        {
            if (spTransformPacket.Type == SlPacketType.ChangePoints)
            {
                //TODO set points
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
                    await transformationService.RemoveSpAsync(clientSession.Character);
                }
                else
                {
                    if ((clientSession.Character.SpPoint == 0) && (clientSession.Character.SpAdditionPoint == 0))
                    {
                        await clientSession.SendPacketAsync(new MsgPacket
                        {
                            Message = gameLanguageLocalizer[LanguageKey.SP_NOPOINTS,
                                clientSession.Account.Language]
                        });
                        return;
                    }

                    if (currentRunningSeconds >= clientSession.Character.SpCooldown)
                    {
                        if (spTransformPacket.Type == SlPacketType.WearSpAndTransform)
                        {
                            await transformationService.ChangeSpAsync(clientSession.Character);
                        }
                        else
                        {
                            await clientSession.SendPacketAsync(new DelayPacket
                            {
                                Type = DelayPacketType.Locomotion,
                                Delay = 5000,
                                Packet = new SpTransformPacket { Type = SlPacketType.WearSpAndTransform }
                            });
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
    }
}
