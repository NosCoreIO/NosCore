//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
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
using System.Collections.Generic;
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

            if (clientSession.Character.Group.Type != GroupType.Group)
            {
                await clientSession.SendPacketAsync(clientSession.Character.GenerateSayi(SayColorType.Red, Game18NConstString.TeammateCanNotOpenShop));
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
                    if (clientSession.Character.Shop != null)
                    {
                        return;
                    }

                    // Built aside and published only once complete, so a buyer never sees a
                    // half-filled shop.
                    var shop = new Shop();
                    var usedSlots = new HashSet<(NoscorePocketType, short)>();
                    sbyte shopSlot = -1;
                    foreach (var item in (mShopPacket.ItemList ?? []).OfType<MShopItemSubPacket>())
                    {
                        shopSlot++;
                        if (item.Amount == 0)
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

                        if (!usedSlots.Add(((NoscorePocketType)item.Type, item.Slot)))
                        {
                            await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.PersonalShop });
                            return;
                        }

                        if (!inv.ItemInstance.Item.IsTradable || (inv.ItemInstance.BoundCharacterId != null))
                        {
                            await clientSession.SendPacketAsync(clientSession.Character.GenerateSayi(SayColorType.Red, Game18NConstString.SomeItemsCannotBeTraded));
                            await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.PersonalShop });
                            return;
                        }

                        shop.ShopItems.TryAdd(shopSlot,
                            new ShopItem
                            {
                                Amount = item.Amount,
                                Price = item.Price,
                                Slot = (byte)shopSlot,
                                Type = 0,
                                ItemInstance = inv.ItemInstance
                            });
                    }

                    if (shop.ShopItems.IsEmpty)
                    {
                        await clientSession.SendPacketAsync(clientSession.Character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NoItemToSell));
                        await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.PersonalShop });
                        return;
                    }

                    shop.OwnerCharacter = clientSession.Character;
                    shop.MenuType = 3;
                    shop.ShopId = 501;
                    shop.Size = 60;

                    for (var i = 0; i < shop.Name.Keys.Count; i++)
                    {
                        var key = shop.Name.Keys.ElementAt(i);
                        shop.Name[key] = string.IsNullOrWhiteSpace(mShopPacket.Name) ?
                            ((byte)Game18NConstString.PrivateShop).ToString() :
                            mShopPacket.Name.Substring(0, Math.Min(mShopPacket.Name.Length, 20));
                    }

                    var owner = clientSession.Character;
                    shop.DialogSubscription = owner.Requests[typeof(NpcDialogRequestSubject)].Subscribe(data =>
                        data.ClientSession.SendPacketAsync(owner.GenerateNInv(shop, 0, 0)));
                    clientSession.Character.Shop = shop;

                    await clientSession.Character.MapInstance.SendPacketAsync(clientSession.Character.GenerateShop(clientSession.Character.AccountLanguage));
                    await clientSession.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.ShopOpened
                    });

                    await clientSession.Character.MapInstance.SendPacketAsync(clientSession.Character.GeneratePFlag(),
                        new EveryoneBut(clientSession.ConnectedChannel.Id));
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
