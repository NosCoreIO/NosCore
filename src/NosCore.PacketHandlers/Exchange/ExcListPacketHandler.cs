//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.BankService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Exchanges;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Exchange
{
    public class ExcListPacketHandler(IExchangeService exchangeService, ILogger<ExcListPacketHandler> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISessionRegistry sessionRegistry)
        : PacketHandler<ExcListPacket>, IWorldPacketHandler
    {
        private static readonly HashSet<PocketType> TradablePockets =
        [
            PocketType.Equipment, PocketType.Main, PocketType.Etc, PocketType.Specialist, PocketType.Costume
        ];

        public override async Task ExecuteAsync(ExcListPacket packet, ClientSession clientSession)
        {
            var character = clientSession.Character;
            var targetId = exchangeService.GetTargetId(character.VisualId);
            if (!targetId.HasValue)
            {
                logger.LogError(logLanguage[LogLanguageKey.USER_NOT_IN_EXCHANGE]);
                return;
            }

            if ((packet.Gold > character.Gold) || (packet.BankGold > character.BankGold / BankUnits.GoldPerUnit))
            {
                logger.LogError(logLanguage[LogLanguageKey.NOT_ENOUGH_GOLD]);
                return;
            }

            var subPacketList = new List<ServerExcListSubPacket?>();
            var offer = new Dictionary<InventoryItemInstance, short>();

            var hasTarget = sessionRegistry.TryGetCharacter(s =>
                (s.VisualId == targetId.Value) &&
                (s.MapInstanceId == character.MapInstanceId), out var target);

            if ((packet.SubPackets?.Count > 0) && hasTarget)
            {
                byte i = 0;
                foreach (var value in packet.SubPackets)
                {
                    var item = value == null || value.Amount <= 0 || value.Slot < 0 || !TradablePockets.Contains(value.PocketType)
                        ? null
                        : character.InventoryService.LoadBySlotAndType(value.Slot, (NoscorePocketType)value.PocketType);

                    if ((item == null) || (item.ItemInstance.Amount < value!.Amount) || offer.ContainsKey(item))
                    {
                        var closeExchange =
                            exchangeService.CloseExchange(character.VisualId,
                                ExchangeResultType.Failure);
                        await clientSession.SendPacketAsync(closeExchange);
                        await target.SendPacketAsync(closeExchange);
                        logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE_LIST]);
                        return;
                    }

                    if (!item.ItemInstance.Item.IsTradable)
                    {
                        var closeExchange = exchangeService.CloseExchange(character.CharacterId,
                            ExchangeResultType.Failure);
                        await clientSession.SendPacketAsync(closeExchange);
                        await target.SendPacketAsync(closeExchange);
                        logger.LogError(
                            logLanguage[LogLanguageKey.CANNOT_TRADE_NOT_TRADABLE_ITEM]);
                        return;
                    }

                    offer[item] = value.Amount;

                    var subPacket = new ServerExcListSubPacket
                    {
                        ExchangeSlot = i,
                        PocketType = value.PocketType,
                        ItemVnum = item.ItemInstance.ItemVNum,
                        Upgrade = item.ItemInstance.Upgrade,
                        AmountOrRare = value.PocketType == PocketType.Equipment ? item.ItemInstance.Rare : value.Amount
                    };


                    subPacketList.Add(subPacket);
                    i++;
                }
            }
            else
            {
                subPacketList.Add(new ServerExcListSubPacket { ExchangeSlot = null });
            }

            if (!exchangeService.SetOffer(character.CharacterId, packet.Gold, packet.BankGold * BankUnits.GoldPerUnit, offer))
            {
                return;
            }

            if (hasTarget)
            {
                await target.SendPacketAsync(
                    character.GenerateServerExcListPacket(packet.Gold, packet.BankGold, subPacketList));
            }
        }
    }
}
