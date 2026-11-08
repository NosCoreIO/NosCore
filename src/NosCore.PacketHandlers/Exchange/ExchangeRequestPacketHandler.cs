//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.Packets.ClientPackets.Exchanges;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Exchange
{
    public class ExchangeRequestPackettHandler(IExchangeService exchangeService, ILogger<ExchangeRequestPackettHandler> logger,
            IBlacklistHub blacklistHttpClient, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry, IGameLanguageLocalizer gameLanguageLocalizer)
        : PacketHandler<ExchangeRequestPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ExchangeRequestPacket packet, ClientSession clientSession)
        {
            var hasTarget = sessionRegistry.TryGetCharacter(s =>
                (s.VisualId == packet.VisualId) &&
                (s.MapInstanceId == clientSession.Character.MapInstanceId), out var target);
            ExcClosePacket closeExchange;

            if (hasTarget && ((packet.RequestType == RequestExchangeType.Confirmed) ||
                (packet.RequestType == RequestExchangeType.Cancelled)))
            {
                logger.LogError(logLanguage[LogLanguageKey.CANT_FIND_CHARACTER]);
                return;
            }

            if (clientSession.Character.InShop || (hasTarget && target.InShop))
            {
                logger.LogError(logLanguage[LogLanguageKey.PLAYER_IN_SHOP]);
                return;
            }

            switch (packet.RequestType)
            {
                case RequestExchangeType.Requested:
                    if (exchangeService.CheckExchange(clientSession.Character.CharacterId) ||
                        exchangeService.CheckExchange(hasTarget ? target.VisualId : 0))
                    {
                        await clientSession.SendPacketAsync(new Infoi2Packet
                        {
                            Message = Game18NConstString.TradingWithSomeoneElse,
                            ArgumentType = 1,
                            Game18NArguments = { hasTarget ? target.Name : "" }
                        });
                        return;
                    }

                    if (!hasTarget || target.ExchangeBlocked || (target.VisualId == clientSession.Character.VisualId))
                    {
                        await clientSession.SendPacketAsync(new Infoi2Packet
                        {
                            Message = Game18NConstString.BlockingTrades,
                            ArgumentType = 1,
                            Game18NArguments = { hasTarget ? target.Name : "" }
                        });
                        return;
                    }

                    if (await IsBlacklistedByAsync(clientSession.Character.VisualId, target.VisualId))
                    {
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Character.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.AlreadyBlacklisted
                        });
                        return;
                    }

                    if (clientSession.Character.InShop || target.InShop)
                    {
                        await clientSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.CanNotTradeShopOwners
                        });
                        return;
                    }

                    if (await IsBlacklistedByAsync(target.VisualId, clientSession.Character.VisualId))
                    {
                        await clientSession.SendPacketAsync(new Infoi2Packet
                        {
                            Message = Game18NConstString.BlockingTrades,
                            ArgumentType = 1,
                            Game18NArguments = { target.Name }
                        });
                        return;
                    }

                    var cooldown = exchangeService.GetRequestCooldown(clientSession.Character);
                    if (cooldown.HasValue)
                    {
                        await clientSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.CannotSendInvite,
                            ArgumentType = 4,
                            Game18NArguments = { (int)Math.Ceiling(cooldown.Value.TotalSeconds) }
                        });
                        return;
                    }

                    exchangeService.RegisterRequest(clientSession.Character, target.VisualId);

                    await clientSession.SendPacketAsync(new Infoi2Packet
                    {
                        Message = Game18NConstString.YouInvitedToTrade,
                        ArgumentType = 1,
                        Game18NArguments = { target.Name }
                    });

                    await target.SendPacketAsync(new Dlgi2Packet
                    {
                        YesPacket = new ExchangeRequestPacket
                        { RequestType = RequestExchangeType.List, VisualId = clientSession.Character.VisualId },
                        NoPacket = new ExchangeRequestPacket
                        { RequestType = RequestExchangeType.Declined, VisualId = clientSession.Character.VisualId },
                        Question = Game18NConstString.WantAcceptTrade,
                        ArgumentType = 2,
                        Game18NArguments = { $"{clientSession.Character.Level}", clientSession.Character.Name ?? "" }
                    });
                    return;

                case RequestExchangeType.List:
                    if (!hasTarget || !exchangeService.TakeRequest(target, clientSession.Character.VisualId))
                    {
                        logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                        return;
                    }

                    if (clientSession.Character.ExchangeBlocked || target.ExchangeBlocked
                        || await IsBlacklistedByAsync(clientSession.Character.VisualId, target.VisualId)
                        || await IsBlacklistedByAsync(target.VisualId, clientSession.Character.VisualId))
                    {
                        return;
                    }

                    if (!exchangeService.OpenExchange(clientSession.Character.VisualId, target.CharacterId))
                    {
                        return;
                    }

                    await clientSession.SendPacketAsync(clientSession.Character.GenerateServerExcListPacket(null, null, null));
                    await target.SendPacketAsync(target.GenerateServerExcListPacket(null, null, null));
                    return;

                case RequestExchangeType.Declined:
                    if (!hasTarget || !exchangeService.TakeRequest(target, clientSession.Character.VisualId))
                    {
                        logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                        return;
                    }

                    await clientSession.SendPacketAsync(new Sayi2Packet
                    {
                        VisualType = VisualType.Player,
                        VisualId = clientSession.Character.CharacterId,
                        Type = SayColorType.Yellow,
                        Message = Game18NConstString.CancelledTrade,
                        ArgumentType = 1,
                        Game18NArguments = { target.Name }
                    });
                    await target.SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = target.CharacterId,
                        Type = SayColorType.Yellow,
                        Message = Game18NConstString.TradeCancelled2
                    });
                    return;

                case RequestExchangeType.Confirmed:
                    var targetId = exchangeService.GetTargetId(clientSession.Character.CharacterId);

                    if (!targetId.HasValue)
                    {
                        logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                        return;
                    }

                    if (!sessionRegistry.TryGetCharacter(s =>
                        (s.VisualId == targetId.Value) && (s.MapInstance == clientSession.Character.MapInstance), out var exchangeTarget))
                    {
                        logger.LogError(logLanguage[LogLanguageKey.CANT_FIND_CHARACTER]);
                        return;
                    }

                    if (!exchangeService.ConfirmExchange(clientSession.Character.VisualId))
                    {
                        await clientSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.TradeWaitingConfirm
                        });
                        return;
                    }

                    var sessionData = exchangeService.GetData(clientSession.Character.CharacterId);
                    var targetData = exchangeService.GetData(exchangeTarget.VisualId);
                    var success = exchangeService.ValidateExchange(clientSession, exchangeTarget);
                    var result = success.Item1;

                    if (result == ExchangeResultType.Failure)
                    {
                        foreach (var infoPacket in success.Item2!)
                        {
                            if (infoPacket.Key == clientSession.Character.CharacterId)
                            {
                                await clientSession.SendPacketAsync(infoPacket.Value);
                            }
                            else if (infoPacket.Key == exchangeTarget.VisualId)
                            {
                                await exchangeTarget.SendPacketAsync(infoPacket.Value);
                            }
                            else
                            {
                                logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                            }
                        }
                    }
                    else
                    {
                        var itemList = await exchangeService.ProcessExchangeAsync(clientSession.Character.VisualId,
                            exchangeTarget.VisualId, clientSession.Character.InventoryService, exchangeTarget.InventoryService);

                        if (itemList == null)
                        {
                            result = ExchangeResultType.Failure;
                        }
                        else
                        {
                            foreach (var item in itemList)
                            {
                                if (item.Key == clientSession.Character.CharacterId)
                                {
                                    await clientSession.SendPacketAsync(item.Value);
                                }
                                else
                                {
                                    await exchangeTarget.SendPacketAsync(item.Value);
                                }
                            }

                            await clientSession.Character.RemoveGoldAsync(sessionData.Gold, gameLanguageLocalizer);
                            clientSession.Character.RemoveBankGold(sessionData.BankGold);

                            await exchangeTarget.AddGoldAsync(sessionData.Gold, gameLanguageLocalizer);
                            exchangeTarget.AddBankGold(sessionData.BankGold);

                            await exchangeTarget.RemoveGoldAsync(targetData.Gold, gameLanguageLocalizer);
                            exchangeTarget.RemoveBankGold(targetData.BankGold);

                            await clientSession.Character.AddGoldAsync(targetData.Gold, gameLanguageLocalizer);
                            clientSession.Character.AddBankGold(targetData.BankGold);
                        }
                    }

                    closeExchange = exchangeService.CloseExchange(clientSession.Character.VisualId, result)!;
                    await exchangeTarget.SendPacketAsync(closeExchange);
                    await clientSession.SendPacketAsync(closeExchange);
                    return;

                case RequestExchangeType.Cancelled:
                    var cancelId = exchangeService.GetTargetId(clientSession.Character.CharacterId);
                    if (!cancelId.HasValue)
                    {
                        logger.LogError(logLanguage[LogLanguageKey.USER_NOT_IN_EXCHANGE]);
                        return;
                    }

                    closeExchange =
                        exchangeService.CloseExchange(clientSession.Character.VisualId, ExchangeResultType.Failure)!;
                    if (sessionRegistry.TryGetCharacter(s => s.VisualId == cancelId.Value, out var cancelTarget))
                    {
                        await cancelTarget.SendPacketAsync(closeExchange);
                    }
                    await clientSession.SendPacketAsync(closeExchange);
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task<bool> IsBlacklistedByAsync(long characterId, long otherId)
        {
            var blacklisted = await blacklistHttpClient.GetBlacklistedAsync(characterId);
            return blacklisted.Any(s => s.CharacterId == otherId);
        }
    }
}
