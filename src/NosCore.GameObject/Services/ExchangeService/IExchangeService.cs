//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.Inventory;
using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.ExchangeService
{
    public interface IExchangeService
    {
        void SetGold(long visualId, long gold, long bankGold);

        Tuple<ExchangeResultType, Dictionary<long, IPacket>?> ValidateExchange(ClientSession session,
            ICharacterEntity targetSession);

        void ConfirmExchange(long visualId);
        bool IsExchangeConfirmed(long visualId);
        ExchangeData GetData(long visualId);
        void AddItems(long visualId, InventoryItemInstance item, short amount);
        bool CheckExchange(long visualId);
        long? GetTargetId(long visualId);
        bool CheckExchange(long visualId, long targetId);
        ExcClosePacket? CloseExchange(long visualId, ExchangeResultType resultType);
        bool OpenExchange(long visualId, long targetVisualId);

        List<KeyValuePair<long, IvnPacket>> ProcessExchange(long firstUser, long secondUser,
            IInventoryService sessionInventory, IInventoryService targetInventory);
    }
}
