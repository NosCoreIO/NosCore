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

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Bazaar;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using System.Linq;
using NosCore.GameObject.Networking;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CScalcPacketHandler(IOptions<WorldConfiguration> worldConfiguration,
            IBazaarHub bazaarHttpClient,
            IItemGenerationService itemProvider, ILogger logger, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ICharacterPacketSystem characterPacketSystem, IInventoryPacketSystem inventoryPacketSystem)
        : PacketHandler<CScalcPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CScalcPacket packet, ClientSession clientSession)
        {
            var bzs = await bazaarHttpClient.GetBazaar(packet.BazaarId, null, null, null, null, null, null, null, null).ConfigureAwait(false);
            var bz = bzs.FirstOrDefault();
            if ((bz != null) && (bz.SellerName == clientSession.Player.Name))
            {
                var soldedamount = bz.BazaarItem!.Amount - bz.ItemInstance!.Amount;
                var taxes = bz.BazaarItem.MedalUsed ? (short)0 : (short)(bz.BazaarItem.Price * 0.10 * soldedamount);
                var price = bz.BazaarItem.Price * soldedamount - taxes;
                if (clientSession.Player.InventoryService.CanAddItem(bz.ItemInstance.ItemVNum))
                {
                    if (clientSession.Player.Gold + price <= worldConfiguration.Value.MaxGoldAmount)
                    {
                        clientSession.Player.SetGold(clientSession.Player.Gold + price);
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Player.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.PurchaseCompleted,
                            ArgumentType = 2,
                            Game18NArguments = { bz.ItemInstance.ItemVNum.ToString(), bz.ItemInstance.Amount }
                        }).ConfigureAwait(false);
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Player.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.PurchaseCompletedWithGoldUsed,
                            ArgumentType = 4,
                            Game18NArguments = { price }
                        }).ConfigureAwait(false);
                        await clientSession.SendPacketAsync(characterPacketSystem.GenerateGold(clientSession.Player)).ConfigureAwait(false);

                        var itemInstance = await itemInstanceDao.FirstOrDefaultAsync(s => s!.Id == bz.ItemInstance.Id).ConfigureAwait(false);
                        if (itemInstance == null)
                        {
                            return;
                        }
                        var item = itemProvider.Convert(itemInstance);
                        item.Id = Guid.NewGuid();

                        var newInv =
                            clientSession.Player.InventoryService.AddItemToPocket(
                                InventoryItemInstance.Create(item, clientSession.Player.CharacterId));
                        var pocketChange = inventoryPacketSystem.GeneratePocketChange(newInv!);
                        if (pocketChange != null)
                        {
                            await clientSession.SendPacketAsync(pocketChange).ConfigureAwait(false);
                        }
                        var remove = await bazaarHttpClient.DeleteBazaarAsync(packet.BazaarId, bz.ItemInstance.Amount,
                            clientSession.Player.Name).ConfigureAwait(false);
                        if (remove)
                        {
                            await clientSession.SendPacketAsync(new RCScalcPacket
                            {
                                Type = VisualType.Player,
                                Price = bz.BazaarItem.Price,
                                RemainingAmount = (short)(bz.BazaarItem.Amount - bz.ItemInstance.Amount),
                                Amount = bz.BazaarItem.Amount,
                                Taxes = taxes,
                                Total = price + taxes
                            }).ConfigureAwait(false);
                            await clientSession.HandlePacketsAsync(new[]
                                {new CSListPacket {Index = 0, Filter = BazaarStatusType.Default}}).ConfigureAwait(false);
                            return;
                        }

                        logger.Error(logLanguage[LogLanguageKey.BAZAAR_DELETE_ERROR]);
                    }
                    else
                    {
                        await clientSession.SendPacketAsync(new MsgiPacket
                        {
                            Type = MessageType.Default,
                            Message = Game18NConstString.MaxGoldReached
                        }).ConfigureAwait(false);
                    }
                }
                else
                {
                    await clientSession.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.NotEnoughSpace
                    }).ConfigureAwait(false);
                }

                await clientSession.SendPacketAsync(new RCScalcPacket
                {
                    Type = VisualType.Player, Price = bz.BazaarItem.Price, RemainingAmount = 0,
                    Amount = bz.BazaarItem.Amount, Taxes = 0, Total = 0
                }).ConfigureAwait(false);
            }
            else
            {
                await clientSession.SendPacketAsync(new RCScalcPacket
                { Type = VisualType.Player, Price = 0, RemainingAmount = 0, Amount = 0, Taxes = 0, Total = 0 }).ConfigureAwait(false);
            }
        }
    }
}