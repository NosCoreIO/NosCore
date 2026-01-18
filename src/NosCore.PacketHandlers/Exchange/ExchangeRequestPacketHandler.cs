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

using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.Packets.ClientPackets.Exchanges;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Exchange
{
    public class ExchangeRequestPackettHandler(IExchangeService exchangeService, ILogger logger,
            IBlacklistHub blacklistHttpClient, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry, ICharacterPacketSystem characterPacketSystem)
        : PacketHandler<ExchangeRequestPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ExchangeRequestPacket packet, ClientSession clientSession)
        {
            var target = sessionRegistry.GetPlayer(s =>
                (s.VisualId == packet.VisualId) &&
                (s.MapInstanceId == clientSession.Player.MapInstanceId));
            ExcClosePacket closeExchange;

            if (!target.HasValue && ((packet.RequestType == RequestExchangeType.Confirmed) ||
                (packet.RequestType == RequestExchangeType.Cancelled)))
            {
                logger.Error(logLanguage[LogLanguageKey.CANT_FIND_CHARACTER]);
                return;
            }

            if (clientSession.Player.InShop || (target.HasValue && target.Value.InShop))
            {
                logger.Error(logLanguage[LogLanguageKey.PLAYER_IN_SHOP]);
                return;
            }

            switch (packet.RequestType)
            {
                case RequestExchangeType.Requested:
                    if (exchangeService.GetExchange(clientSession.Player.CharacterId) != null ||
                        exchangeService.GetExchange(target.HasValue ? target.Value.VisualId : 0) != null)
                    {
                        await clientSession.SendPacketAsync(new Infoi2Packet
                        {
                            Message = Game18NConstString.TradingWithSomeoneElse,
                            ArgumentType = 1,
                            Game18NArguments = { target.HasValue ? target.Value.Name : "" }
                        }).ConfigureAwait(false);
                        return;
                    }

                    if (!target.HasValue || target.Value.CharacterData.ExchangeBlocked)
                    {
                        await clientSession.SendPacketAsync(new Infoi2Packet
                        {
                            Message = Game18NConstString.BlockingTrades,
                            ArgumentType = 1,
                            Game18NArguments = { target.HasValue ? target.Value.Name : "" }
                        }).ConfigureAwait(false);
                        return;
                    }

                    var targetPlayer = target.Value;
                    var blacklisteds = await blacklistHttpClient.GetBlacklistedAsync(clientSession.Player.VisualId).ConfigureAwait(false);
                    if (blacklisteds.Any(s => s.CharacterId == targetPlayer.VisualId))
                    {
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Player.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.AlreadyBlacklisted
                        }).ConfigureAwait(false);
                        return;
                    }

                    if (clientSession.Player.InShop || targetPlayer.InShop)
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
                        ArgumentType = 1,
                        Game18NArguments = { targetPlayer.Name }
                    }).ConfigureAwait(false);

                    var targetSenderRequest = sessionRegistry.GetSenderByCharacterId(targetPlayer.VisualId);
                    if (targetSenderRequest != null)
                    {
                        await targetSenderRequest.SendPacketAsync(new Dlgi2Packet
                        {
                            YesPacket = new ExchangeRequestPacket
                            { RequestType = RequestExchangeType.List, VisualId = clientSession.Player.VisualId },
                            NoPacket = new ExchangeRequestPacket
                            { RequestType = RequestExchangeType.Declined, VisualId = clientSession.Player.VisualId },
                            Question = Game18NConstString.WantAcceptTrade,
                            ArgumentType = 2,
                            Game18NArguments = { $"{clientSession.Player.Level}", clientSession.Player.Name }
                        }).ConfigureAwait(false);
                    }
                    return;

                case RequestExchangeType.List:
                    var exchange = exchangeService.OpenExchange(clientSession.Player.VisualId, target.HasValue ? target.Value.CharacterId : 0);
                    if (exchange == null)
                    {
                        return;
                    }

                    await clientSession.SendPacketAsync(characterPacketSystem.GenerateServerExcListPacket(clientSession.Player, null, null, null)).ConfigureAwait(false);
                    if (target.HasValue)
                    {
                        var targetSenderList = sessionRegistry.GetSenderByCharacterId(target.Value.VisualId);
                        if (targetSenderList != null)
                        {
                            await targetSenderList.SendPacketAsync(characterPacketSystem.GenerateServerExcListPacket(target.Value, null, null, null)).ConfigureAwait(false);
                        }
                    }
                    return;

                case RequestExchangeType.Declined:
                    await clientSession.SendPacketAsync(new Sayi2Packet
                    {
                        VisualType = VisualType.Player,
                        VisualId = clientSession.Player.CharacterId,
                        Type = SayColorType.Yellow,
                        Message = Game18NConstString.CancelledTrade,
                        ArgumentType = 1,
                        Game18NArguments = { target.HasValue ? target.Value.Name : "" }
                    }).ConfigureAwait(false);
                    if (target.HasValue)
                    {
                        var targetSenderDeclined = sessionRegistry.GetSenderByCharacterId(target.Value.VisualId);
                        if (targetSenderDeclined != null)
                        {
                            await targetSenderDeclined.SendPacketAsync(new SayiPacket
                            {
                                VisualType = VisualType.Player,
                                VisualId = target.Value.CharacterId,
                                Type = SayColorType.Yellow,
                                Message = Game18NConstString.TradeCancelled2
                            }).ConfigureAwait(false);
                        }
                    }
                    return;

                case RequestExchangeType.Confirmed:
                    var currentExchange = exchangeService.GetExchange(clientSession.Player.CharacterId);
                    if (currentExchange == null)
                    {
                        logger.Error(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                        return;
                    }

                    var targetId = currentExchange.GetPartnerId(clientSession.Player.CharacterId);
                    var exchangeTargetNullable = sessionRegistry.GetPlayer(s =>
                        (s.VisualId == targetId) && (s.MapInstance == clientSession.Player.MapInstance));

                    if (!exchangeTargetNullable.HasValue)
                    {
                        logger.Error(logLanguage[LogLanguageKey.CANT_FIND_CHARACTER]);
                        return;
                    }

                    var exchangeTarget = exchangeTargetNullable.Value;
                    await currentExchange.AcquireLockAsync().ConfigureAwait(false);
                    try
                    {
                        currentExchange.Confirm(clientSession.Player.VisualId);

                        if (!currentExchange.BothConfirmed)
                        {
                            await clientSession.SendPacketAsync(new InfoiPacket
                            {
                                Message = Game18NConstString.TradeWaitingConfirm
                            }).ConfigureAwait(false);
                            return;
                        }

                        var exchangeTargetSender = sessionRegistry.GetSenderByCharacterId(exchangeTarget.VisualId);
                        var success = exchangeService.ValidateExchange(clientSession, exchangeTarget, currentExchange);

                        if (success.Item1 == ExchangeResultType.Success)
                        {
                            foreach (var infoPacket in success.Item2!)
                            {
                                if (infoPacket.Key == clientSession.Player.CharacterId)
                                {
                                    await clientSession.SendPacketAsync(infoPacket.Value).ConfigureAwait(false);
                                }
                                else if (infoPacket.Key == exchangeTarget.VisualId && exchangeTargetSender != null)
                                {
                                    await exchangeTargetSender.SendPacketAsync(infoPacket.Value).ConfigureAwait(false);
                                }
                                else
                                {
                                    logger.Error(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                                }
                            }
                        }
                        else
                        {
                            var itemList = exchangeService.ProcessExchange(clientSession.Player.VisualId,
                                exchangeTarget.VisualId, clientSession.Player.InventoryService,
                                exchangeTarget.InventoryService, currentExchange);

                            foreach (var item in itemList)
                            {
                                if (item.Key == clientSession.Player.CharacterId)
                                {
                                    await clientSession.SendPacketAsync(item.Value).ConfigureAwait(false);
                                }
                                else if (exchangeTargetSender != null)
                                {
                                    await exchangeTargetSender.SendPacketAsync(item.Value).ConfigureAwait(false);
                                }
                            }

                            var getSessionData = currentExchange.GetPlayerData(clientSession.Player.CharacterId);
                            var sessionCurrentGold = clientSession.Player.Gold;
                            clientSession.Player.SetGold(sessionCurrentGold - getSessionData.Gold);
                            clientSession.Account.BankMoney -= getSessionData.BankGold * 1000;

                            var targetCurrentGold = exchangeTarget.Gold;
                            exchangeTarget.SetGold(targetCurrentGold + getSessionData.Gold);
                            exchangeTarget.Account.BankMoney += getSessionData.BankGold * 1000;

                            var getTargetData = currentExchange.GetPlayerData(exchangeTarget.VisualId);
                            exchangeTarget.SetGold(exchangeTarget.Gold - getTargetData.Gold);
                            exchangeTarget.Account.BankMoney -= getTargetData.BankGold * 1000;

                            clientSession.Player.SetGold(clientSession.Player.Gold + getTargetData.Gold);
                            clientSession.Account.BankMoney += getTargetData.BankGold * 1000;
                        }

                        closeExchange = exchangeService.CloseExchange(clientSession.Player.VisualId, success.Item1)!;
                        if (exchangeTargetSender != null)
                        {
                            await exchangeTargetSender.SendPacketAsync(closeExchange).ConfigureAwait(false);
                        }
                        await clientSession.SendPacketAsync(closeExchange).ConfigureAwait(false);
                    }
                    finally
                    {
                        currentExchange.ReleaseLock();
                    }
                    return;

                case RequestExchangeType.Cancelled:
                    var cancelExchange = exchangeService.GetExchange(clientSession.Player.CharacterId);
                    if (cancelExchange == null)
                    {
                        logger.Error(logLanguage[LogLanguageKey.USER_NOT_IN_EXCHANGE]);
                        return;
                    }

                    var cancelTargetId = cancelExchange.GetPartnerId(clientSession.Player.CharacterId);
                    var cancelTarget = sessionRegistry.GetPlayer(s => s.VisualId == cancelTargetId);

                    closeExchange = exchangeService.CloseExchange(clientSession.Player.VisualId, ExchangeResultType.Failure)!;
                    if (cancelTarget.HasValue)
                    {
                        var cancelTargetSender = sessionRegistry.GetSenderByCharacterId(cancelTarget.Value.VisualId);
                        if (cancelTargetSender != null)
                        {
                            await cancelTargetSender.SendPacketAsync(closeExchange).ConfigureAwait(false);
                        }
                    }
                    await clientSession.SendPacketAsync(closeExchange).ConfigureAwait(false);
                    return;

                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }
    }
}
