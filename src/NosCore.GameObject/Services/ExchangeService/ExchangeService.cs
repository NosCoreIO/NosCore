//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ExchangeService
{
    public class ExchangeService(IOptions<WorldConfiguration> worldConfiguration, ILogger<ExchangeService> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry, IClock clock)
        : IExchangeService
    {
        public static readonly Duration RequestLifetime = Duration.FromSeconds(30);

        public static readonly Duration RequestCooldown = Duration.FromSeconds(5);

        // Every claim ends in a close: the settling side waits this long at most for the partner's
        // packet lock and closes the trade whatever happens, which is what a leaving player waits for.
        public static readonly TimeSpan PartnerLockWait = TimeSpan.FromSeconds(5);

        private readonly object _openLock = new();

        public Duration? GetRequestCooldown(PlayerComponentBundle requester)
        {
            if (requester.LastExchangeRequest is not { } last)
            {
                return null;
            }

            var remaining = last + RequestCooldown - clock.GetCurrentInstant();
            return remaining > Duration.Zero ? remaining : null;
        }

        public void RegisterRequest(PlayerComponentBundle requester, long targetId)
        {
            var now = clock.GetCurrentInstant();
            requester.ExchangeRequestCharacterIds[targetId] = now;
            requester.LastExchangeRequest = now;
        }

        public bool TakeRequest(PlayerComponentBundle requester, long targetId)
        {
            return requester.ExchangeRequestCharacterIds.TryRemove(targetId, out var requestedAt)
                && clock.GetCurrentInstant() - requestedAt <= RequestLifetime;
        }

        public bool SetOffer(PlayerComponentBundle player, long gold, long bankGold,
            IReadOnlyDictionary<InventoryItemInstance, short> items)
        {
            var data = player.Exchange;
            if (data == null)
            {
                logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                return false;
            }

            var partner = data.Partner;

            lock (data.Settlement)
            {
                if (data.Settlement.IsClaimed)
                {
                    return false;
                }

                data.ExchangeItems = new ConcurrentDictionary<InventoryItemInstance, short>(items);
                data.Gold = gold;
                data.BankGold = bankGold;
                data.ExchangeConfirmed = false;
                partner.ExchangeConfirmed = false;
                return true;
            }
        }

        public Tuple<ExchangeResultType, Dictionary<long, IPacket>?> ValidateExchange(ClientSession session,
            PlayerComponentBundle targetSession, ExchangeData exchangeInfo, ExchangeData targetInfo)
        {
            var dictionary = new Dictionary<long, IPacket>();

            if (exchangeInfo.Gold > session.Character.Gold || exchangeInfo.BankGold > session.Character.BankGold)
            {
                dictionary.Add(session.Character.CharacterId, new InfoiPacket
                {
                    Message = exchangeInfo.Gold > session.Character.Gold
                        ? Game18NConstString.NotEnoughGold
                        : Game18NConstString.NotEnoughFounds
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (targetInfo.Gold > targetSession.Gold || targetInfo.BankGold > targetSession.BankGold)
            {
                dictionary.Add(targetSession.VisualId, new InfoiPacket
                {
                    Message = targetInfo.Gold > targetSession.Gold
                        ? Game18NConstString.NotEnoughGold
                        : Game18NConstString.NotEnoughFounds
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (exchangeInfo.Gold + targetSession.Gold > worldConfiguration.Value.MaxGoldAmount)
            {
                dictionary.Add(targetSession.VisualId, new InfoiPacket
                {
                    Message = Game18NConstString.MaxGoldReached
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (targetInfo.Gold + session.Character.Gold > worldConfiguration.Value.MaxGoldAmount)
            {
                dictionary.Add(session.Character.CharacterId, new InfoiPacket
                {
                    Message = Game18NConstString.MaxGoldReached
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (exchangeInfo.BankGold + targetSession.BankGold > worldConfiguration.Value.MaxBankGoldAmount)
            {
                dictionary.Add(targetSession.VisualId, new InfoiPacket
                {
                    Message = Game18NConstString.CanNotDepositAnymore
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (targetInfo.BankGold + session.Character.BankGold > worldConfiguration.Value.MaxBankGoldAmount)
            {
                dictionary.Add(session.Character.CharacterId, new InfoiPacket
                {
                    Message = Game18NConstString.CanNotDepositAnymore
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            if (exchangeInfo.ExchangeItems.Keys.Any(s => !s.ItemInstance.Item.IsTradable))
            {
                dictionary.Add(session.Character.CharacterId, new InfoiPacket
                {
                    Message = Game18NConstString.ItemCanNotBeSold
                });
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                    dictionary);
            }

            var targetFirstItem = targetInfo.ExchangeItems.Keys.FirstOrDefault();
            var exchangeFirstItem = exchangeInfo.ExchangeItems.Keys.FirstOrDefault();

            var targetEnoughPlace = targetFirstItem == null || session.Character.InventoryService.EnoughPlace(
                targetInfo.ExchangeItems.Keys.Select(s => s.ItemInstance).ToList(),
                targetFirstItem.Type);

            var exchangeEnoughPlace = exchangeFirstItem == null || targetSession.InventoryService.EnoughPlace(
                exchangeInfo.ExchangeItems.Keys.Select(s => s.ItemInstance).ToList(),
                exchangeFirstItem.Type);

            if (targetEnoughPlace && exchangeEnoughPlace)
            {
                return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Success, null);
            }

            dictionary.Add(session.Character.CharacterId, new InfoiPacket
            {
                Message = Game18NConstString.FullInventory
            });
            dictionary.Add(targetSession.VisualId, new InfoiPacket
            {
                Message = Game18NConstString.FullInventory
            });
            return new Tuple<ExchangeResultType, Dictionary<long, IPacket>?>(ExchangeResultType.Failure,
                dictionary);

        }

        public ClaimedExchange? ConfirmExchange(PlayerComponentBundle player)
        {
            var data = player.Exchange;
            if (data == null)
            {
                return null;
            }

            var partner = data.Partner;

            lock (data.Settlement)
            {
                if (data.Settlement.IsClaimed)
                {
                    return null;
                }

                data.ExchangeConfirmed = true;
                if (!partner.ExchangeConfirmed)
                {
                    return null;
                }

                data.Settlement.IsClaimed = true;
                return new ClaimedExchange(data, partner);
            }
        }

        public ExcClosePacket? CancelExchange(PlayerComponentBundle player)
        {
            var settlement = player.Exchange?.Settlement;
            if (settlement == null)
            {
                logger.LogError(logLanguage[LogLanguageKey.USER_NOT_IN_EXCHANGE]);
                return null;
            }

            lock (settlement)
            {
                if (settlement.IsClaimed)
                {
                    return null;
                }

                settlement.IsClaimed = true;
            }

            return CloseExchange(player, ExchangeResultType.Failure);
        }

        public async Task<ExcClosePacket?> LeaveExchangeAsync(PlayerComponentBundle player)
        {
            var settlement = player.Exchange?.Settlement;
            if (settlement == null)
            {
                return null;
            }

            bool settling;
            lock (settlement)
            {
                settling = settlement.IsClaimed;
                settlement.IsClaimed = true;
            }

            if (settling)
            {
                await settlement.Closed.Task;
            }

            return player.InExchange ? CloseExchange(player, ExchangeResultType.Failure) : null;
        }

        public ExcClosePacket? CloseExchange(PlayerComponentBundle player, ExchangeResultType resultType)
        {
            ExchangeSettlement settlement;
            lock (_openLock)
            {
                var data = player.Exchange;
                if (data == null)
                {
                    logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                    return null;
                }

                settlement = data.Settlement;
                lock (settlement)
                {
                    settlement.IsClaimed = true;
                }

                player.Exchange = null;
                if (sessionRegistry.TryGetCharacter(c => c.VisualId == data.PartnerId, out var partner))
                {
                    partner.Exchange = null;
                }
            }

            settlement.Closed.TrySetResult();
            return new ExcClosePacket
            {
                Type = resultType
            };
        }

        public bool OpenExchange(PlayerComponentBundle initiator, PlayerComponentBundle target)
        {
            lock (_openLock)
            {
                if (initiator.InExchange || target.InExchange)
                {
                    logger.LogError(logLanguage[LogLanguageKey.ALREADY_EXCHANGE]);
                    return false;
                }

                var (initiatorData, targetData) = ExchangeData.Pair(initiator.VisualId, target.VisualId);
                initiator.Exchange = initiatorData;
                target.Exchange = targetData;
            }

            return true;
        }
    }
}
