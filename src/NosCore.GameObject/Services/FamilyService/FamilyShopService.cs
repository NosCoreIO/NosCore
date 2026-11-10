//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.FamilyUpgradeService;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyShopService(List<FamilyUpgradeDto> familyUpgrades, List<ItemDto> items,
        IFamilyUpgradeService capacities, IFamilyHub familyHub, IPubSubHub pubSubHub, IFamilyService familyService)
        : IFamilyShopService
    {
        /// <summary>The shopping window the family manager's dialog opens.</summary>
        public const byte ShopType = 48;

        private List<FamilyUpgradeDto> Offered => familyUpgrades
            .Where(s => Enum.IsDefined(s.UpgradeType))
            .OrderBy(s => s.ItemVNum)
            .ToList();

        public Task OpenAsync(ClientSession session, IAliveEntity manager)
        {
            var family = session.Character.Family;
            return family == null ? Task.CompletedTask : session.SendPacketAsync(GenerateList(manager));
        }

        public async Task BuyAsync(ClientSession session, IAliveEntity manager, short slot)
        {
            var family = session.Character.Family;
            var offered = Offered;
            if (family == null || slot < 0 || slot >= offered.Count)
            {
                return;
            }

            var upgrade = offered[slot];
            FamilyExtensionPurchase? refusal = family.AuthorityOf(session.Character.CharacterId) != FamilyAuthority.Head
                ? FamilyExtensionPurchase.NotHead
                : family.IsOwned(upgrade, capacities)
                    ? FamilyExtensionPurchase.AlreadyPurchased
                    : family.FamilyLevel < upgrade.FamilyLevelMinimum
                        ? FamilyExtensionPurchase.FamilyLevelInsufficient
                        : !family.HasPrevious(upgrade, capacities)
                            ? FamilyExtensionPurchase.PreviousNotPurchased
                            : null;
            if (refusal != null)
            {
                await SendRefusalAsync(session, refusal.Value);
                return;
            }

            var price = items.First(s => s.VNum == upgrade.ItemVNum).Price;
            if (session.Character.Gold < price)
            {
                await session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.InsufficientGoldAvailable });
                return;
            }

            var result = await familyHub.PurchaseExtensionAsync(family.FamilyId, session.Character.CharacterId, upgrade.ItemVNum);
            if (result != FamilyExtensionPurchase.Purchased)
            {
                await SendRefusalAsync(session, result);
                return;
            }

            session.Character.Gold -= price;
            await session.Character.AddGoldAsync();
            await session.SendPacketAsync(new Msgi2Packet
            {
                Type = MessageType.Default,
                Message = Game18NConstString.PurchasedItemX,
                ArgumentType = 2,
                Game18NArguments = { upgrade.ItemVNum.ToString() }
            });
            await pubSubHub.SendMessageAsync(new FamilyChangedData { FamilyId = family.FamilyId });

            session.Character.Family = await familyService.GetFamilyAsync(session.Character.CharacterId);
        }

        private NInvPacket GenerateList(IAliveEntity manager) => new()
        {
            VisualType = manager.VisualType,
            VisualId = manager.VisualId,
            ShopKind = 100,
            Unknown = 0,
            Items = Offered.Select((upgrade, slot) =>
            {
                var item = items.First(s => s.VNum == upgrade.ItemVNum);
                return (NInvItemSubPacket?)new NInvItemSubPacket
                {
                    Type = (PocketType)item.Type,
                    Slot = (short)slot,
                    Price = (int)item.Price,
                    RareAmount = 1,
                    VNum = item.VNum
                };
            }).ToList()
        };

        private static Task SendRefusalAsync(ClientSession session, FamilyExtensionPurchase refusal)
        {
            Game18NConstString? message = refusal switch
            {
                FamilyExtensionPurchase.NotHead => Game18NConstString.HeadCanPurchaseOnly,
                FamilyExtensionPurchase.AlreadyPurchased => Game18NConstString.AlreadyPurchased,
                FamilyExtensionPurchase.FamilyLevelInsufficient => Game18NConstString.FamilyLevelInsufficient,
                FamilyExtensionPurchase.PreviousNotPurchased => Game18NConstString.RequestToPurchaseStandardVersionFirst,
                _ => null
            };
            return message == null
                ? Task.CompletedTask
                : session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = message.Value });
        }
    }
}
