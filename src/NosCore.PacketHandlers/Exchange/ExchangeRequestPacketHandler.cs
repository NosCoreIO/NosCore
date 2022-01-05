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
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.Packets.ClientPackets.Exchanges;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.PacketHandlers.Exchange
{
    public class ExchangeRequestPackettHandler : PacketHandler<ExchangeRequestPacket>, IWorldPacketHandler
    {
        private readonly IBlacklistHttpClient _blacklistHttpClient;
        private readonly IExchangeService _exchangeProvider;
        private readonly ILogger _logger;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public ExchangeRequestPackettHandler(IExchangeService exchangeService, ILogger logger,
            IBlacklistHttpClient blacklistHttpClient, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _exchangeProvider = exchangeService;
            _logger = logger;
            _blacklistHttpClient = blacklistHttpClient;
            _logLanguage = logLanguage;
        }

        public override async Task ExecuteAsync(ExchangeRequestPacket packet, ClientSession clientSession)
        {
            var target = Broadcaster.Instance.GetCharacter(s =>
                (s.VisualId == packet.VisualId) &&
                (s.MapInstanceId == clientSession.Character.MapInstanceId)) as Character;
            ExcClosePacket closeExchange;

            if ((target != null) && ((packet.RequestType == RequestExchangeType.Confirmed) ||
                (packet.RequestType == RequestExchangeType.Cancelled)))
            {
                _logger.Error(_logLanguage[LogLanguageKey.CANT_FIND_CHARACTER]);
                return;
            }

            if (clientSession.Character.InShop || (target?.InShop ?? false))
            {
                _logger.Error(_logLanguage[LogLanguageKey.PLAYER_IN_SHOP]);
                return;
            }

            switch (packet.RequestType)
            {
                case RequestExchangeType.Requested:
                    if (_exchangeProvider.CheckExchange(clientSession.Character.CharacterId) ||
                        _exchangeProvider.CheckExchange(target?.VisualId ?? 0))
                    {
                        await clientSession.SendPacketAsync(new Infoi2Packet
                        {
                            Message = Game18NConstString.TradingWithSomeoneElse,
                            FirstArgument = 1,
                            SecondArgument = target?.Name ?? ""
                        }).ConfigureAwait(false);
                        return;
                    }

                    if (target?.ExchangeBlocked ?? true)
                    {
                        await clientSession.SendPacketAsync(new Infoi2Packet
                        {
                            Message = Game18NConstString.BlockingTrades,
                            FirstArgument = 1,
                            SecondArgument = target?.Name ?? ""
                        }).ConfigureAwait(false);
                        return;
                    }

                    var blacklisteds = await _blacklistHttpClient.GetBlackListsAsync(clientSession.Character.VisualId).ConfigureAwait(false);
                    if (blacklisteds.Any(s => s.CharacterId == target.VisualId))
                    {
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Character.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.AlreadyBlacklisted
                        }).ConfigureAwait(false);
                        return;
                    }

                    if (clientSession.Character.InShop || target.InShop)
                    {
                        await clientSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.CanNotTradeShopOwners
                        }).ConfigureAwait(false);
                        return;
                    }

                    await clientSession.SendPacketAsync(new Infoi2Packet
                    {
                        Message = Game18NConstString.YouInvitedToTrade,
                        FirstArgument = 1,
                        SecondArgument = target.Name
                    }).ConfigureAwait(false);

                    await target.SendPacketAsync(new DlgPacket
                    {
                        YesPacket = new ExchangeRequestPacket
                        { RequestType = RequestExchangeType.List, VisualId = clientSession.Character.VisualId },
                        NoPacket = new ExchangeRequestPacket
                        { RequestType = RequestExchangeType.Declined, VisualId = clientSession.Character.VisualId },
                        Question = string.Format(GameLanguage.Instance.GetMessageFromKey(LanguageKey.INCOMING_EXCHANGE,
                            clientSession.Account.Language), clientSession.Character.Name)
                    }).ConfigureAwait(false);
                    return;

                case RequestExchangeType.List:
                    if (!_exchangeProvider.OpenExchange(clientSession.Character.VisualId, target?.CharacterId ?? 0))
                    {
                        return;
                    }

                    await clientSession.SendPacketAsync(clientSession.Character.GenerateServerExcListPacket(null, null, null)).ConfigureAwait(false);
                    await (target == null ? Task.CompletedTask : target.SendPacketAsync(target.GenerateServerExcListPacket(null, null, null))).ConfigureAwait(false);
                    return;

                case RequestExchangeType.Declined:
                    await clientSession.SendPacketAsync(clientSession.Character.GenerateSay(
                        GameLanguage.Instance.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED,
                            clientSession.Account.Language),
                        SayColorType.Yellow)).ConfigureAwait(false);
                    await (target == null ? Task.CompletedTask : target.SendPacketAsync(target.GenerateSay(target.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED),
                        SayColorType.Yellow))).ConfigureAwait(false);
                    return;

                case RequestExchangeType.Confirmed:
                    var targetId = _exchangeProvider.GetTargetId(clientSession.Character.CharacterId);

                    if (!targetId.HasValue)
                    {
                        _logger.Error(_logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                        return;
                    }

                    var exchangeTarget = Broadcaster.Instance.GetCharacter(s =>
                        (s.VisualId == targetId.Value) && (s.MapInstance == clientSession.Character.MapInstance));

                    if (exchangeTarget == null)
                    {
                        _logger.Error(_logLanguage[LogLanguageKey.CANT_FIND_CHARACTER]);
                        return;
                    }

                    _exchangeProvider.ConfirmExchange(clientSession.Character.VisualId);

                    if (!_exchangeProvider.IsExchangeConfirmed(clientSession.Character.VisualId) ||
                        !_exchangeProvider.IsExchangeConfirmed(exchangeTarget.VisualId))
                    {
                        await clientSession.SendPacketAsync(new InfoPacket
                        {
                            Message = string.Format(GameLanguage.Instance.GetMessageFromKey(LanguageKey.IN_WAITING_FOR,
                                clientSession.Account.Language), exchangeTarget.Name)
                        }).ConfigureAwait(false);
                        return;
                    }

                    var success = _exchangeProvider.ValidateExchange(clientSession, exchangeTarget);

                    if (success.Item1 == ExchangeResultType.Success)
                    {
                        foreach (var infoPacket in success.Item2!)
                        {
                            if (infoPacket.Key == clientSession.Character.CharacterId)
                            {
                                await clientSession.SendPacketAsync(infoPacket.Value).ConfigureAwait(false);
                            }
                            else if (infoPacket.Key == exchangeTarget.VisualId)
                            {
                                await exchangeTarget.SendPacketAsync(infoPacket.Value).ConfigureAwait(false);
                            }
                            else
                            {
                                _logger.Error(_logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                            }
                        }
                    }
                    else
                    {
                        var itemList = _exchangeProvider.ProcessExchange(clientSession.Character.VisualId,
                            exchangeTarget.VisualId, clientSession.Character.InventoryService, exchangeTarget.InventoryService);

                        foreach (var item in itemList)
                        {
                            if (item.Key == clientSession.Character.CharacterId)
                            {
                                await clientSession.SendPacketAsync(item.Value).ConfigureAwait(false);
                            }
                            else
                            {
                                await exchangeTarget.SendPacketAsync(item.Value).ConfigureAwait(false);
                            }
                        }

                        var getSessionData = _exchangeProvider.GetData(clientSession.Character.CharacterId);
                        await clientSession.Character.RemoveGoldAsync(getSessionData.Gold).ConfigureAwait(false);
                        clientSession.Character.RemoveBankGold(getSessionData.BankGold * 1000);

                        await exchangeTarget.AddGoldAsync(getSessionData.Gold).ConfigureAwait(false);
                        exchangeTarget.AddBankGold(getSessionData.BankGold * 1000);

                        var getTargetData = _exchangeProvider.GetData(exchangeTarget.VisualId);
                        await exchangeTarget.RemoveGoldAsync(getTargetData.Gold).ConfigureAwait(false);
                        exchangeTarget.RemoveBankGold(getTargetData.BankGold * 1000);

                        await clientSession.Character.AddGoldAsync(getTargetData.Gold).ConfigureAwait(false);
                        clientSession.Character.AddBankGold(getTargetData.BankGold * 1000);
                    }

                    closeExchange = _exchangeProvider.CloseExchange(clientSession.Character.VisualId, success.Item1)!;
                    await exchangeTarget.SendPacketAsync(closeExchange).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(closeExchange).ConfigureAwait(false);
                    return;

                case RequestExchangeType.Cancelled:
                    var cancelId = _exchangeProvider.GetTargetId(clientSession.Character.CharacterId);
                    if (!cancelId.HasValue)
                    {
                        _logger.Error(_logLanguage[LogLanguageKey.USER_NOT_IN_EXCHANGE]);
                        return;
                    }

                    var cancelTarget = Broadcaster.Instance.GetCharacter(s => s.VisualId == cancelId.Value);

                    closeExchange =
                        _exchangeProvider.CloseExchange(clientSession.Character.VisualId, ExchangeResultType.Failure)!;
                    cancelTarget?.SendPacketAsync(closeExchange);
                    await clientSession.SendPacketAsync(closeExchange).ConfigureAwait(false);
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}