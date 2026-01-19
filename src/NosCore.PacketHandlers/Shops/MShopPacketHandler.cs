//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NRunService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup.ChannelMatcher;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class MShopPacketHandler(IHeuristic distanceCalculator) : PacketHandler<MShopPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(MShopPacket mShopPacket, ClientSession clientSession)
        {
            var portal = clientSession.Character.MapInstance.Portals.Find(port =>
            distanceCalculator.GetDistance((clientSession.Character.PositionX, clientSession.Character.PositionY), (port.SourceX, port.SourceY)) <= 6);

            if (portal != null)
            {
                await clientSession.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.OpenShopAwayPortal
                });
                return;
            }

            if ((clientSession.Character.Group != null) && (clientSession.Character.Group?.Type != GroupType.Group))
            {
                await clientSession.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = clientSession.Character.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.TeammateCanNotOpenShop
                });
                return;
            }

            if (!clientSession.Character.MapInstance.ShopAllowed)
            {
                await clientSession.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UseCommercialMapToShop
                });
                await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.PersonalShop });
                return;
            }

            switch (mShopPacket.Type)
            {
                case CreateShopPacketType.Open:
                    clientSession.Character.Shop = new Shop();
                    sbyte shopSlot = -1;
                    foreach (var item in mShopPacket.ItemList!)
                    {
                        shopSlot++;
                        if (item!.Amount == 0)
                        {
                            continue;
                        }

                        var inv = clientSession.Character.InventoryService.LoadBySlotAndType(item.Slot,
                            (NoscorePocketType)item.Type);
                        if (inv == null)
                        {
                            //log
                            continue;
                        }

                        if (inv.ItemInstance.Amount < item.Amount || item.Amount <= 0)
                        {
                            //todo log
                            return;
                        }

                        if (item.Price < 0)
                        {
                            return;
                        }

                        if (!inv.ItemInstance.Item.IsTradable || (inv.ItemInstance.BoundCharacterId != null))
                        {
                            await clientSession.SendPacketAsync(new SayiPacket
                            {
                                VisualType = VisualType.Player,
                                VisualId = clientSession.Character.CharacterId,
                                Type = SayColorType.Red,
                                Message = Game18NConstString.SomeItemsCannotBeTraded
                            });
                            await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.PersonalShop });
                            clientSession.Character.Shop = null;
                            return;
                        }

                        clientSession.Character.Shop.ShopItems.TryAdd(shopSlot,
                            new ShopItem
                            {
                                Amount = item.Amount,
                                Price = item.Price,
                                Slot = (byte)shopSlot,
                                Type = 0,
                                ItemInstance = inv.ItemInstance
                            });
                    }

                    if (clientSession.Character.Shop.ShopItems.IsEmpty)
                    {
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Character.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.NoItemToSell
                        });
                        await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.PersonalShop });
                        clientSession.Character.Shop = null;
                        return;
                    }

                    clientSession.Character.Shop.OwnerCharacter = clientSession.Character;
                    clientSession.Character.Shop.MenuType = 3;
                    clientSession.Character.Shop.ShopId = 501;
                    clientSession.Character.Shop.Size = 60;

                    for (var i = 0; i < clientSession.Character.Shop.Name.Keys.Count; i++)
                    {
                        var key = clientSession.Character.Shop.Name.Keys.ElementAt(i);
                        clientSession.Character.Shop.Name[key] = string.IsNullOrWhiteSpace(mShopPacket.Name) ?
                            ((byte)Game18NConstString.PrivateShop).ToString() :
                            mShopPacket.Name.Substring(0, Math.Min(mShopPacket.Name.Length, 20));
                    }

                    await clientSession.Character.MapInstance.SendPacketAsync(clientSession.Character.GenerateShop(clientSession.Character.AccountLanguage));
                    await clientSession.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.ShopOpened
                    });

                    clientSession.Character.Requests[typeof(INrunEventHandler)].Subscribe(data =>
                        data.ClientSession.SendPacketAsync(
                            clientSession.Character.GenerateNpcReq(clientSession.Character.Shop.ShopId)));
                    await clientSession.Character.MapInstance.SendPacketAsync(clientSession.Character.GeneratePFlag(),
                        new EveryoneBut(clientSession.Channel!.Id));
                    clientSession.Character.IsSitting = true;
                    await clientSession.SendPacketAsync(clientSession.Character.GenerateCond());
                    await clientSession.Character.MapInstance.SendPacketAsync(clientSession.Character.GenerateRest());
                    break;
                case CreateShopPacketType.Close:
                    await clientSession.Character.CloseShopAsync();
                    break;
                case CreateShopPacketType.Create:
                    await clientSession.SendPacketAsync(new IshopPacket());
                    break;
                default:
                    //todo log
                    return;
            }
        }
    }
}
