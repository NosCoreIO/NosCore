//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Bazaar;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CRegPacketHandler(IOptions<WorldConfiguration> configuration, IBazaarHub bazaarHttpClient,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao)
        : PacketHandler<CRegPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CRegPacket cRegPacket, ClientSession clientSession)
        {
            if (cRegPacket.Amount <= 0 || cRegPacket.Price < 0)
            {
                return;
            }

            var medal = clientSession.Character.StaticBonusList.FirstOrDefault(s =>
                (s.StaticBonusType == StaticBonusType.BazaarMedalGold) ||
                (s.StaticBonusType == StaticBonusType.BazaarMedalSilver));

            if (cRegPacket.Price > long.MaxValue / cRegPacket.Amount)
            {
                return;
            }

            var price = cRegPacket.Price * cRegPacket.Amount;
            var taxmax = price > 100000 ? price / 200 : 500;
            var taxmin = price >= 4000 ? 60 + (price - 4000) / 2000 * 30 > 10000 ? 10000
                : 60 + (price - 4000) / 2000 * 30 : 50;
            var tax = medal == null ? taxmax : taxmin;
            var maxGold = configuration.Value.MaxGoldAmount;
            if (clientSession.Character.Gold < tax)
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughGold
                });
                return;
            }

            var it = clientSession.Character.InventoryService.LoadBySlotAndType(cRegPacket.Slot,
                cRegPacket.Inventory == 4 ? 0 : (NoscorePocketType)cRegPacket.Inventory);
            if ((it?.ItemInstance == null) || !it.ItemInstance.Item.IsSoldable || (it.ItemInstance.BoundCharacterId != null) ||
                (cRegPacket.Amount > it.ItemInstance.Amount))
            {
                return;
            }

            var maxPrice = medal == null ? 1000000 : maxGold;

            if (price > maxPrice)
            {
                await clientSession.SendPacketAsync(new ModaliPacket
                {
                    Type = 1,
                    Message = Game18NConstString.NotExceedMaxPrice,
                    ArgumentType = 4,
                    Game18NArguments = { maxPrice }
                });
                return;
            }

            if ((medal == null) && (cRegPacket.Durability > 1))
            {
                return;
            }

            short duration;
            switch (cRegPacket.Durability)
            {
                case 1:
                    duration = 24;
                    break;

                case 2:
                    duration = 168;
                    break;

                case 3:
                    duration = 360;
                    break;

                case 4:
                    duration = 720;
                    break;

                default:
                    return;
            }

            var bazar = clientSession.Character.InventoryService.LoadBySlotAndType(cRegPacket.Slot,
                cRegPacket.Inventory == 4 ? NoscorePocketType.Equipment : (NoscorePocketType)cRegPacket.Inventory);
            if (bazar?.ItemInstance == null)
            {
                return;
            }
            IItemInstanceDto bazaaritem = bazar.ItemInstance;
            bazaaritem = (await itemInstanceDao.TryInsertOrUpdateAsync(bazaaritem))!;

            var result = await bazaarHttpClient.AddBazaarAsync(new BazaarRequest
            {
                ItemInstanceId = bazaaritem.Id,
                CharacterId = clientSession.Character.CharacterId,
                CharacterName = clientSession.Character.Name,
                HasMedal = medal != null,
                Price = cRegPacket.Price,
                IsPackage = cRegPacket.IsPackage,
                Duration = duration,
                Amount = cRegPacket.Amount
            });

            switch (result)
            {
                case LanguageKey.LIMIT_EXCEEDED:
                    await clientSession.SendPacketAsync(new ModaliPacket
                    {
                        Type = 1,
                        Message = Game18NConstString.ListedMaxItemsNumber
                    });
                    break;

                case LanguageKey.OBJECT_IN_BAZAAR:
                    if (bazar.ItemInstance.Amount == cRegPacket.Amount)
                    {
                        await inventoryItemInstanceDao.TryDeleteAsync(bazar.Id);
                        clientSession.Character.InventoryService.DeleteById(bazar.ItemInstanceId);
                    }
                    else
                    {
                        clientSession.Character.InventoryService.RemoveItemAmountFromInventory(cRegPacket.Amount,
                            bazar.ItemInstanceId);
                    }

                    await clientSession.SendPacketAsync(((InventoryItemInstance?)null).GeneratePocketChange(
                        cRegPacket.Inventory == 4 ? PocketType.Equipment : (PocketType)cRegPacket.Inventory,
                        cRegPacket.Slot));
                    clientSession.Character.Gold -= tax;
                    await clientSession.SendPacketAsync(clientSession.Character.GenerateGold());
                    await clientSession.SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = clientSession.Character.CharacterId,
                        Type = SayColorType.Yellow,
                        Message = Game18NConstString.ItemAddedToBazar
                    });
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.ItemAddedToBazar
                    });

                    await clientSession.SendPacketAsync(new RCRegPacket { Type = VisualType.Player });
                    break;
            }
        }
    }
}
