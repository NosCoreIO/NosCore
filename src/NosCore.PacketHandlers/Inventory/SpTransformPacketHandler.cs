//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Networking;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking;


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
                if (clientSession.Player.IsSitting)
                {
                    return;
                }

                if (!(clientSession.Player.InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance is SpecialistInstance specialistInstance))
                {
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.NoSpecialistCardEquipped
                    }).ConfigureAwait(false);
                    return;
                }

                if (clientSession.Player.IsVehicled)
                {
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.CantUseInVehicle
                    }).ConfigureAwait(false);
                    return;
                }

                var currentRunningSeconds = (clock.GetCurrentInstant() - clientSession.Player.LastSp).TotalSeconds;

                if (clientSession.Player.UseSp)
                {
                    var player = clientSession.Player;
                    player.LastSp = clock.GetCurrentInstant();
                    await transformationService.RemoveSpAsync(clientSession.Player);
                }
                else
                {
                    if ((clientSession.Player.SpPoint == 0) && (clientSession.Player.SpAdditionPoint == 0))
                    {
                        await clientSession.SendPacketAsync(new MsgPacket
                        {
                            Message = gameLanguageLocalizer[LanguageKey.SP_NOPOINTS,
                                clientSession.Account.Language]
                        }).ConfigureAwait(false);
                        return;
                    }

                    if (currentRunningSeconds >= clientSession.Player.SpCooldown)
                    {
                        if (spTransformPacket.Type == SlPacketType.WearSpAndTransform)
                        {
                            await transformationService.ChangeSpAsync(clientSession.Player);
                        }
                        else
                        {
                            await clientSession.SendPacketAsync(new DelayPacket
                            {
                                Type = DelayPacketType.Locomotion,
                                Delay = 5000,
                                Packet = new SpTransformPacket { Type = SlPacketType.WearSpAndTransform }
                            }).ConfigureAwait(false);
                            await clientSession.Player.MapInstance.SendPacketAsync(new GuriPacket
                            {
                                Type = GuriPacketType.Dance,
                                Argument = 1,
                                EntityId = clientSession.Player.CharacterId
                            }).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await clientSession.SendPacketAsync(new MsgiPacket
                        {
                            Type = MessageType.Default,
                            Message = Game18NConstString.CantTrasformWithSideEffect,
                            ArgumentType = 4,
                            Game18NArguments = { (short)(clientSession.Player.SpCooldown - (int)Math.Round(currentRunningSeconds)) }
                        }).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}