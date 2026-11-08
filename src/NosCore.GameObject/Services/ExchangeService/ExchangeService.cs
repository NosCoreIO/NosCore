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
            IExchangeRequestRegistry exchangeRegistry, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
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

        public bool SetOffer(long visualId, long gold, long bankGold,
            IReadOnlyDictionary<InventoryItemInstance, short> items)
        {
            var data = exchangeRegistry.GetExchangeData(visualId);
            var targetId = GetTargetId(visualId);
            var partner = targetId.HasValue ? exchangeRegistry.GetExchangeData(targetId.Value) : null;
            if (data == null || partner == null)
            {
                logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                return false;
            }

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
            ICharacterEntity targetSession, ExchangeData exchangeInfo, ExchangeData targetInfo)
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
                    Message = Game18NConstString.FullInventory
                });
            }

            if (targetInfo.Gold + session.Character.Gold > worldConfiguration.Value.MaxGoldAmount)
            {
                dictionary.Add(targetSession.VisualId, new InfoiPacket
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

            var targetHasItems = targetFirstItem != null;
            var exchangeHasItems = exchangeFirstItem != null;

            var targetEnoughPlace = !targetHasItems || session.Character.InventoryService.EnoughPlace(
                targetInfo.ExchangeItems.Keys.Select(s => s.ItemInstance).ToList(),
                targetFirstItem!.Type);

            var exchangeEnoughPlace = !exchangeHasItems || targetSession.InventoryService.EnoughPlace(
                exchangeInfo.ExchangeItems.Keys.Select(s => s.ItemInstance).ToList(),
                exchangeFirstItem!.Type);

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

        public ClaimedExchange? ConfirmExchange(long visualId)
        {
            var data = exchangeRegistry.GetExchangeData(visualId);
            var targetId = GetTargetId(visualId);
            var partner = targetId.HasValue ? exchangeRegistry.GetExchangeData(targetId.Value) : null;
            if (data == null || partner == null)
            {
                return null;
            }

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

        public ExcClosePacket? CancelExchange(long visualId)
        {
            var settlement = exchangeRegistry.GetExchangeData(visualId)?.Settlement;
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

            return CloseExchange(visualId, ExchangeResultType.Failure);
        }

        public async Task<ExcClosePacket?> LeaveExchangeAsync(long visualId)
        {
            var settlement = exchangeRegistry.GetExchangeData(visualId)?.Settlement;
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

            return GetTargetId(visualId).HasValue ? CloseExchange(visualId, ExchangeResultType.Failure) : null;
        }

        public bool IsExchangeConfirmed(long visualId)
        {
            return exchangeRegistry.GetExchangeData(visualId)?.ExchangeConfirmed ?? false;
        }

        public ExchangeData GetData(long visualId)
        {
            return exchangeRegistry.GetExchangeData(visualId) ?? new ExchangeData();
        }

        public bool CheckExchange(long visualId)
        {
            return exchangeRegistry.HasExchange(visualId);
        }

        public long? GetTargetId(long visualId)
        {
            var pair = exchangeRegistry.GetExchangeRequestPair(visualId);
            if (pair == null)
            {
                return null;
            }

            return pair.Value.Value == visualId ? pair.Value.Key : pair.Value.Value;
        }

        public bool CheckExchange(long visualId, long targetId)
        {
            var pair1 = exchangeRegistry.GetExchangeRequestPair(visualId);
            var pair2 = exchangeRegistry.GetExchangeRequestPair(targetId);
            return (pair1 != null && pair1.Value.Key == visualId && pair1.Value.Value == visualId) ||
                   (pair2 != null && pair2.Value.Key == targetId && pair2.Value.Value == targetId);
        }

        public ExcClosePacket? CloseExchange(long visualId, ExchangeResultType resultType)
        {
            KeyValuePair<long, long> data;
            ExchangeSettlement? settlement;
            lock (_openLock)
            {
                var pair = exchangeRegistry.GetExchangeRequestPair(visualId);
                if (pair == null)
                {
                    logger.LogError(logLanguage[LogLanguageKey.INVALID_EXCHANGE]);
                    return null;
                }

                data = pair.Value;
                settlement = exchangeRegistry.GetExchangeData(data.Key)?.Settlement;
                if (settlement != null)
                {
                    lock (settlement)
                    {
                        settlement.IsClaimed = true;
                    }
                }

                if (!exchangeRegistry.RemoveExchangeData(data.Key) || !exchangeRegistry.RemoveExchangeRequest(data.Key))
                {
                    logger.LogError(logLanguage[LogLanguageKey.TRY_REMOVE_FAILED], data.Key);
                }

                if (!exchangeRegistry.RemoveExchangeData(data.Value) || !exchangeRegistry.RemoveExchangeRequest(data.Value))
                {
                    logger.LogError(logLanguage[LogLanguageKey.TRY_REMOVE_FAILED], data.Value);
                }
            }

            SetInExchange(data.Key, false);
            SetInExchange(data.Value, false);
            settlement?.Closed.TrySetResult();

            return new ExcClosePacket
            {
                Type = resultType
            };
        }

        // The lifecycle owns the flag. Set at the seven call sites instead, it was simply
        // never set at all, which left every InExchangeOrShop guard inert.
        private void SetInExchange(long visualId, bool value)
        {
            if (sessionRegistry.TryGetCharacter(c => c.VisualId == visualId, out var character))
            {
                character.InExchange = value;
            }
        }

        public bool OpenExchange(long visualId, long targetVisualId)
        {
            lock (_openLock)
            {
                if (CheckExchange(visualId) || CheckExchange(targetVisualId))
                {
                    logger.LogError(logLanguage[LogLanguageKey.ALREADY_EXCHANGE]);
                    return false;
                }

                var settlement = new ExchangeSettlement();
                exchangeRegistry.SetExchangeRequest(visualId, targetVisualId);
                exchangeRegistry.SetExchangeData(visualId, new ExchangeData { Settlement = settlement });
                exchangeRegistry.SetExchangeData(targetVisualId, new ExchangeData { Settlement = settlement });
            }

            SetInExchange(visualId, true);
            SetInExchange(targetVisualId, true);
            return true;
        }
    }
}
