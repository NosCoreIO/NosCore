//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.ComponentEntities.Extensions;
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
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Exchange
{
    public class ExchangeRequestPackettHandler(IExchangeService exchangeService, ILogger logger,
            IBlacklistHub blacklistHttpClient, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry)
        : PacketHandler<ExchangeRequestPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ExchangeRequestPacket packet, ClientSession clientSession)
        {
            var target = sessionRegistry.GetCharacter(s =>
                (s.VisualId == packet.VisualId) &&
                (s.MapInstanceId == clientSession.Character.MapInstanceId)) as Character;
            ExcClosePacket closeExchange;

            if ((target != null) && ((packet.RequestType == RequestExchangeType.Confirmed) ||
                (packet.RequestType == RequestExchangeType.Cancelled)))
            {
                logger.Error(logLanguage[LogLanguageKey.CANT_FIND_CHARACTER]);
                return;
            }

            if (clientSession.Character.InShop || (target?.InShop ?? false))
            {
                logger.Error(logLanguage[LogLanguageKey.PLAYER_IN_SHOP]);
                return;
            }

            switch (packet.RequestType)
            {
                case RequestExchangeType.Requested:
                    if (exchangeService.CheckExchange(clientSession.Character.CharacterId) ||
                        exchangeService.CheckExchange(target?.VisualId ?? 0))
                    {
                        await clientSession.SendPacketAsync(new Infoi2Packet
                        {
                            Message = Game18NConstString.TradingWithSomeoneElse,
                            ArgumentType = 1,
                            Game18NArguments = { target?.Name ?? "" }
                        });
                        return;
                    }

                    if (target?.ExchangeBlocked ?? true)
                    {
                        await clientSession.SendPacketAsync(new Infoi2Packet
                        {
                            Message = Game18NConstString.BlockingTrades,
                            ArgumentType = 1,
                            Game18NArguments = { target?.Name ?? "" }
                        });
                        return;
                    }

                    var blacklisteds = await blacklistHttpClient.GetBlacklistedAsync(clientSession.Character.VisualId);
                    if (blacklisteds.Any(s => s.CharacterId == target.VisualId))
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
                    if (!exchangeService.OpenExchange(clientSession.Character.VisualId, target?.CharacterId ?? 0))
                    {
                        return;
                    }

                    await clientSession.SendPacketAsync(clientSession.Character.GenerateServerExcListPacket(null, null, null));
                    await (target == null ? Task.CompletedTask : target.SendPacketAsync(target.GenerateServerExcListPacket(null, null, null)));
                    return;

                case RequestExchangeType.Declined:
                    await clientSession.SendPacketAsync(new Sayi2Packet
                    {
                        VisualType = VisualType.Player,
                        VisualId = clientSession.Character.CharacterId,
                        Type = SayColorType.Yellow,
                        Message = Game18NConstString.CancelledTrade,
                        ArgumentType = 1,
                        Game18NArguments = { target?.Name ?? "" }
                    });
                    await (target == null ? Task.CompletedTask : target.SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = target.CharacterId,
                        Type = SayColorType.Yellow,
                        Message = Game18NConstString.TradeCancelled2
                    }));
                    return;

                case RequestExchangeType.Confirmed:
                    var targetId = exchangeService.GetTargetId(clientSession.Character.CharacterId);

                    if (!targetId.HasValue)
                    {
                        logger.Error(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                        return;
                    }

                    var exchangeTarget = sessionRegistry.GetCharacter(s =>
                        (s.VisualId == targetId.Value) && (s.MapInstance == clientSession.Character.MapInstance));

                    if (exchangeTarget == null)
                    {
                        logger.Error(logLanguage[LogLanguageKey.CANT_FIND_CHARACTER]);
                        return;
                    }

                    exchangeService.ConfirmExchange(clientSession.Character.VisualId);

                    if (!exchangeService.IsExchangeConfirmed(clientSession.Character.VisualId) ||
                        !exchangeService.IsExchangeConfirmed(exchangeTarget.VisualId))
                    {
                        await clientSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.TradeWaitingConfirm
                        });
                        return;
                    }

                    var success = exchangeService.ValidateExchange(clientSession, exchangeTarget);

                    if (success.Item1 == ExchangeResultType.Success)
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
                                logger.Error(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                            }
                        }
                    }
                    else
                    {
                        var itemList = exchangeService.ProcessExchange(clientSession.Character.VisualId,
                            exchangeTarget.VisualId, clientSession.Character.InventoryService, exchangeTarget.InventoryService);

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

                        var getSessionData = exchangeService.GetData(clientSession.Character.CharacterId);
                        await clientSession.Character.RemoveGoldAsync(getSessionData.Gold);
                        clientSession.Character.RemoveBankGold(getSessionData.BankGold * 1000);

                        await exchangeTarget.AddGoldAsync(getSessionData.Gold);
                        exchangeTarget.AddBankGold(getSessionData.BankGold * 1000);

                        var getTargetData = exchangeService.GetData(exchangeTarget.VisualId);
                        await exchangeTarget.RemoveGoldAsync(getTargetData.Gold);
                        exchangeTarget.RemoveBankGold(getTargetData.BankGold * 1000);

                        await clientSession.Character.AddGoldAsync(getTargetData.Gold);
                        clientSession.Character.AddBankGold(getTargetData.BankGold * 1000);
                    }

                    closeExchange = exchangeService.CloseExchange(clientSession.Character.VisualId, success.Item1)!;
                    await exchangeTarget.SendPacketAsync(closeExchange);
                    await clientSession.SendPacketAsync(closeExchange);
                    return;

                case RequestExchangeType.Cancelled:
                    var cancelId = exchangeService.GetTargetId(clientSession.Character.CharacterId);
                    if (!cancelId.HasValue)
                    {
                        logger.Error(logLanguage[LogLanguageKey.USER_NOT_IN_EXCHANGE]);
                        return;
                    }

                    var cancelTarget = sessionRegistry.GetCharacter(s => s.VisualId == cancelId.Value);

                    closeExchange =
                        exchangeService.CloseExchange(clientSession.Character.VisualId, ExchangeResultType.Failure)!;
                    cancelTarget?.SendPacketAsync(closeExchange);
                    await clientSession.SendPacketAsync(closeExchange);
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
