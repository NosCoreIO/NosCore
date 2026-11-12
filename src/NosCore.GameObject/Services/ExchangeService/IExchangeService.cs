//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Exchanges;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ExchangeService
{
    public sealed record ClaimedExchange(ExchangeData Own, ExchangeData Partner);

    public interface IExchangeService
    {
        Duration? GetRequestCooldown(PlayerComponentBundle requester);
        void RegisterRequest(PlayerComponentBundle requester, long targetId);
        bool TakeRequest(PlayerComponentBundle requester, long targetId);

        bool SetOffer(long visualId, long gold, long bankGold, IReadOnlyDictionary<InventoryItemInstance, short> items);

        Tuple<ExchangeResultType, Dictionary<long, IPacket>?> ValidateExchange(ClientSession session,
            PlayerComponentBundle targetSession, ExchangeData exchangeInfo, ExchangeData targetInfo);

        // Records the confirmation. Returns both sides' offers to the one call that completes the
        // pair, which then owns the settlement; every other call gets null.
        ClaimedExchange? ConfirmExchange(long visualId);

        // Closes the exchange as failed unless it has already been claimed for settlement.
        ExcClosePacket? CancelExchange(long visualId);

        // Closes the exchange of a player who is leaving. A trade already settling is waited for
        // until the settlement closes it, so the leaving player's save and cleanup only run once it
        // has ended; returns null when the settlement closed it.
        Task<ExcClosePacket?> LeaveExchangeAsync(long visualId);
        bool IsExchangeConfirmed(long visualId);
        ExchangeData GetData(long visualId);
        bool CheckExchange(long visualId);
        long? GetTargetId(long visualId);
        bool CheckExchange(long visualId, long targetId);
        ExcClosePacket? CloseExchange(long visualId, ExchangeResultType resultType);
        bool OpenExchange(long visualId, long targetVisualId);
    }
}
