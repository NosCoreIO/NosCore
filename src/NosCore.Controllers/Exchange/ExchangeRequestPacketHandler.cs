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

using System;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Exchanges;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ExchangeProvider;
using Serilog;

namespace NosCore.PacketHandlers.Exchange
{
    public class ExchangeRequestPackettHandler : PacketHandler<ExchangeRequestPacket>, IWorldPacketHandler
    {
        private readonly IBlacklistHttpClient _blacklistHttpClient;
        private readonly IExchangeProvider _exchangeProvider;
        private readonly ILogger _logger;

        public ExchangeRequestPackettHandler(IExchangeProvider exchangeProvider, ILogger logger,
            IBlacklistHttpClient blacklistHttpClient)
        {
            _exchangeProvider = exchangeProvider;
            _logger = logger;
            _blacklistHttpClient = blacklistHttpClient;
        }

        public override async Task Execute(ExchangeRequestPacket packet, ClientSession clientSession)
        {
            var target = Broadcaster.Instance.GetCharacter(s =>
                (s.VisualId == packet.VisualId) &&
                (s.MapInstanceId == clientSession.Character.MapInstanceId)) as Character;
            ExcClosePacket closeExchange;

            if ((target != null) && ((packet.RequestType == RequestExchangeType.Confirmed) ||
                (packet.RequestType == RequestExchangeType.Cancelled)))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_FIND_CHARACTER));
                return;
            }

            if (clientSession.Character.InShop || (target?.InShop ?? false))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PLAYER_IN_SHOP));
                return;
            }

            switch (packet.RequestType)
            {
                case RequestExchangeType.Requested:
                    if (_exchangeProvider.CheckExchange(clientSession.Character.CharacterId) ||
                        _exchangeProvider.CheckExchange(target.VisualId))
                    {
                        await clientSession.SendPacket(new MsgPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_EXCHANGE,
                                clientSession.Account.Language),
                            Type = MessageType.White
                        });
                        return;
                    }

                    if (target.ExchangeBlocked)
                    {
                        await clientSession.SendPacket(clientSession.Character.GenerateSay(
                            Language.Instance.GetMessageFromKey(LanguageKey.EXCHANGE_BLOCKED,
                                clientSession.Account.Language),
                            SayColorType.Purple));
                        return;
                    }

                    var blacklisteds = await _blacklistHttpClient.GetBlackLists(clientSession.Character.VisualId);
                    if (blacklisteds.Any(s => s.CharacterId == target.VisualId))
                    {
                        await clientSession.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                                clientSession.Account.Language)
                        });
                        return;
                    }

                    if (clientSession.Character.InShop || target.InShop)
                    {
                        await clientSession.SendPacket(new MsgPacket
                        {
                            Message =
                                Language.Instance.GetMessageFromKey(LanguageKey.HAS_SHOP_OPENED,
                                    clientSession.Account.Language),
                            Type = MessageType.White
                        });
                        return;
                    }

                    await clientSession.SendPacket(new ModalPacket
                    {
                        Message = string.Format(Language.Instance.GetMessageFromKey(LanguageKey.YOU_ASK_FOR_EXCHANGE,
                            clientSession.Account.Language), target.Name),
                        Type = 0
                    });

                    await target.SendPacket(new DlgPacket
                    {
                        YesPacket = new ExchangeRequestPacket
                            {RequestType = RequestExchangeType.List, VisualId = clientSession.Character.VisualId},
                        NoPacket = new ExchangeRequestPacket
                            {RequestType = RequestExchangeType.Declined, VisualId = clientSession.Character.VisualId},
                        Question = string.Format(Language.Instance.GetMessageFromKey(LanguageKey.INCOMING_EXCHANGE,
                            clientSession.Account.Language), clientSession.Character.Name)
                    });
                    return;

                case RequestExchangeType.List:
                    if (!_exchangeProvider.OpenExchange(clientSession.Character.VisualId, target.CharacterId))
                    {
                        return;
                    }

                    await clientSession.SendPacket(clientSession.Character.GenerateServerExcListPacket(null, null, null));
                    await target.SendPacket(target.GenerateServerExcListPacket(null, null, null));
                    return;

                case RequestExchangeType.Declined:
                    await clientSession.SendPacket(clientSession.Character.GenerateSay(
                        Language.Instance.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED,
                            clientSession.Account.Language),
                        SayColorType.Yellow));
                    await (target == null ? Task.CompletedTask : target.SendPacket(target.GenerateSay(target.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED),
                        SayColorType.Yellow)));
                    return;

                case RequestExchangeType.Confirmed:
                    var targetId = _exchangeProvider.GetTargetId(clientSession.Character.CharacterId);

                    if (!targetId.HasValue)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_EXCHANGE));
                        return;
                    }

                    var exchangeTarget = Broadcaster.Instance.GetCharacter(s =>
                        (s.VisualId == targetId.Value) && (s.MapInstance == clientSession.Character.MapInstance));

                    if (exchangeTarget == null)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_FIND_CHARACTER));
                        return;
                    }

                    _exchangeProvider.ConfirmExchange(clientSession.Character.VisualId);

                    if (!_exchangeProvider.IsExchangeConfirmed(clientSession.Character.VisualId) ||
                        !_exchangeProvider.IsExchangeConfirmed(exchangeTarget.VisualId))
                    {
                        await clientSession.SendPacket(new InfoPacket
                        {
                            Message = string.Format(Language.Instance.GetMessageFromKey(LanguageKey.IN_WAITING_FOR,
                                clientSession.Account.Language), exchangeTarget.Name)
                        });
                        return;
                    }

                    var success = _exchangeProvider.ValidateExchange(clientSession, exchangeTarget);

                    if (success.Item1 == ExchangeResultType.Success)
                    {
                        foreach (var infoPacket in success.Item2)
                        {
                            if (infoPacket.Key == clientSession.Character.CharacterId)
                            {
                                await clientSession.SendPacket(infoPacket.Value);
                            }
                            else if (infoPacket.Key == exchangeTarget.VisualId)
                            {
                                await exchangeTarget.SendPacket(infoPacket.Value);
                            }
                            else
                            {
                                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_EXCHANGE));
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
                                await clientSession.SendPacket(item.Value);
                            }
                            else
                            {
                                await exchangeTarget.SendPacket(item.Value);
                            }
                        }

                        var getSessionData = _exchangeProvider.GetData(clientSession.Character.CharacterId);
                        await clientSession.Character.RemoveGold(getSessionData.Gold);
                        clientSession.Character.RemoveBankGold(getSessionData.BankGold * 1000);

                        await exchangeTarget.AddGold(getSessionData.Gold);
                        exchangeTarget.AddBankGold(getSessionData.BankGold * 1000);

                        var getTargetData = _exchangeProvider.GetData(exchangeTarget.VisualId);
                        await exchangeTarget.RemoveGold(getTargetData.Gold);
                        exchangeTarget.RemoveBankGold(getTargetData.BankGold * 1000);

                        await clientSession.Character.AddGold(getTargetData.Gold);
                        clientSession.Character.AddBankGold(getTargetData.BankGold * 1000);
                    }

                    closeExchange = _exchangeProvider.CloseExchange(clientSession.Character.VisualId, success.Item1);
                    exchangeTarget?.SendPacket(closeExchange);
                    await clientSession.SendPacket(closeExchange);
                    return;

                case RequestExchangeType.Cancelled:
                    var cancelId = _exchangeProvider.GetTargetId(clientSession.Character.CharacterId);
                    if (!cancelId.HasValue)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.USER_NOT_IN_EXCHANGE));
                        return;
                    }

                    var cancelTarget = Broadcaster.Instance.GetCharacter(s => s.VisualId == cancelId.Value);

                    closeExchange =
                        _exchangeProvider.CloseExchange(clientSession.Character.VisualId, ExchangeResultType.Failure);
                    cancelTarget?.SendPacket(closeExchange);
                    await clientSession.SendPacket(closeExchange);
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}