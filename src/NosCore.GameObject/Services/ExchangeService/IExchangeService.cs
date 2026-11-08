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
using NosCore.Packets.ServerPackets.Inventory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ExchangeService
{
    public interface IExchangeService
    {
        Duration? GetRequestCooldown(PlayerComponentBundle requester);
        void RegisterRequest(PlayerComponentBundle requester, long targetId);
        bool TakeRequest(PlayerComponentBundle requester, long targetId);

        bool SetOffer(long visualId, long gold, long bankGold, IReadOnlyDictionary<InventoryItemInstance, short> items);

        Tuple<ExchangeResultType, Dictionary<long, IPacket>?> ValidateExchange(ClientSession session,
            ICharacterEntity targetSession);

        bool ConfirmExchange(long visualId);
        bool IsExchangeConfirmed(long visualId);
        ExchangeData GetData(long visualId);
        bool CheckExchange(long visualId);
        long? GetTargetId(long visualId);
        bool CheckExchange(long visualId, long targetId);
        ExcClosePacket? CloseExchange(long visualId, ExchangeResultType resultType);
        bool OpenExchange(long visualId, long targetVisualId);

        Task<List<KeyValuePair<long, IvnPacket>>?> ProcessExchangeAsync(long firstUser, long secondUser,
            IInventoryService sessionInventory, IInventoryService targetInventory);
    }
}
