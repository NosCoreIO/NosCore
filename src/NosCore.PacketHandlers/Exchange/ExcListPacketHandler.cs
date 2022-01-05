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
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.Packets.ClientPackets.Exchanges;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Shared.I18N;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Exchange
{
    public class ExcListPacketHandler : PacketHandler<ExcListPacket>, IWorldPacketHandler
    {
        private readonly IExchangeService _exchangeProvider;
        private readonly ILogger _logger;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public ExcListPacketHandler(IExchangeService exchangeService, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _exchangeProvider = exchangeService;
            _logger = logger;
            _logLanguage = logLanguage;
        }

        public override async Task ExecuteAsync(ExcListPacket packet, ClientSession clientSession)
        {
            if ((packet.Gold > clientSession.Character.Gold) || (packet.BankGold > clientSession.Account.BankMoney))
            {
                _logger.Error(_logLanguage[LogLanguageKey.NOT_ENOUGH_GOLD]);
                return;
            }

            var subPacketList = new List<ServerExcListSubPacket?>();

            var target = Broadcaster.Instance.GetCharacter(s =>
                (s.VisualId == _exchangeProvider.GetTargetId(clientSession.Character.VisualId)) &&
                (s.MapInstanceId == clientSession.Character.MapInstanceId)) as Character;

            if ((packet.SubPackets!.Count > 0) && (target != null))
            {
                byte i = 0;
                foreach (var value in packet.SubPackets)
                {
                    var item = clientSession.Character.InventoryService.LoadBySlotAndType(value!.Slot,
                        (NoscorePocketType)value.PocketType);

                    if ((item == null) || (item.ItemInstance!.Amount < value.Amount))
                    {
                        var closeExchange =
                            _exchangeProvider.CloseExchange(clientSession.Character.VisualId,
                                ExchangeResultType.Failure);
                        await clientSession.SendPacketAsync(closeExchange).ConfigureAwait(false);
                        await target.SendPacketAsync(closeExchange).ConfigureAwait(false);
                        _logger.Error(_logLanguage[LogLanguageKey.INVALID_EXCHANGE_LIST]);
                        return;
                    }

                    if (!item.ItemInstance.Item!.IsTradable)
                    {
                        await clientSession.SendPacketAsync(_exchangeProvider.CloseExchange(clientSession.Character.CharacterId,
                            ExchangeResultType.Failure)).ConfigureAwait(false);
                        await target.SendPacketAsync(_exchangeProvider.CloseExchange(target.VisualId, ExchangeResultType.Failure)).ConfigureAwait(false);
                        _logger.Error(
                            _logLanguage[LogLanguageKey.CANNOT_TRADE_NOT_TRADABLE_ITEM]);
                        return;
                    }

                    _exchangeProvider.AddItems(clientSession.Character.CharacterId, item, value.Amount);

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

            _exchangeProvider.SetGold(clientSession.Character.CharacterId, packet.Gold, packet.BankGold);
            await (target == null ? Task.CompletedTask : target.SendPacketAsync(
                clientSession.Character.GenerateServerExcListPacket(packet.Gold, packet.BankGold, subPacketList))).ConfigureAwait(false);
        }
    }
}