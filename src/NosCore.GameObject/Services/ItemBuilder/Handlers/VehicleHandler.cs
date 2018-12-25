using System;
using System.Collections.Generic;
using System.Text;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.ItemBuilder.Handlers
{
    public class VehicleHandler : IHandler<Item.Item, Tuple<IItemInstance, UseItemPacket>>
    {
        public bool Condition(Item.Item item) => item.ItemType == ItemType.Special && item.Effect == 1000;

        public void Execute(RequestData<Tuple<IItemInstance, UseItemPacket>> requestData)
        {
            var itemInstance = requestData.Data.Item1;
            var packet = requestData.Data.Item2;
            if (requestData.ClientSession.Character.InExchangeOrTrade)
            {
                return;
            }

            if (packet.Mode == 1 && !requestData.ClientSession.Character.IsVehicled)
            {
                requestData.ClientSession.SendPacket(new DelayPacket
                {
                    Type = 3,
                    Delay = 3000,
                    Packet = requestData.ClientSession.Character.GenerateUseItem(itemInstance.Type, itemInstance.Slot, 2, 0)
                });
                return;
            }

            if (packet.Mode == 2 && !requestData.ClientSession.Character.IsVehicled)
            {
                requestData.ClientSession.Character.IsVehicled = true;
                requestData.ClientSession.Character.VehicleSpeed = itemInstance.Item.Speed;
                requestData.ClientSession.Character.MorphUpgrade = 0;
                requestData.ClientSession.Character.MorphDesign = 0;
                requestData.ClientSession.Character.Morph =
                    (short)((short)requestData.ClientSession.Character.Gender + itemInstance.Item.Morph);
                requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(requestData.ClientSession.Character.GenerateEff(196));
                requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(requestData.ClientSession.Character.GenerateCMode());
                requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateCond());
                return;
            }

            requestData.ClientSession.Character.RemoveVehicle();
        }
    }
}
