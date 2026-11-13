//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.WarehouseService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Warehouse;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using FamilyLogType = NosCore.Data.Enumerations.Family.FamilyLogType;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyWarehouseService(IWarehouseTransferService transfer, IFamilyLogService familyLogService, IClock clock)
        : IFamilyWarehouseService
    {
        public async Task OpenAsync(ClientSession session)
        {
            var family = session.Character.Family;
            if (family == null || family.WarehouseSize == 0 || !CanPut(session, family))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotQualifiedForWarehouse });
                return;
            }

            var items = await transfer.GetItemsAsync(family.FamilyId, WarehouseType.FamilyWareHouse);
            await session.SendPacketAsync(new FStashAllPacket
            {
                WarehouseSize = family.WarehouseSize,
                Items = items.Select(s => s.Item.GenerateWarehouseSubPacket(s.Slot)).ToList()
            });
        }

        public Task OpenHistoryAsync(ClientSession session)
        {
            var family = session.Character.Family;
            if (family == null || !family.CanGetHistory(session.Character.CharacterId))
            {
                return session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotAuthorized });
            }

            return session.SendPacketAsync(GenerateFslog(familyLogService.GetWarehouseHistory(family.FamilyId), clock.GetCurrentInstant()));
        }

        public async Task DepositAsync(ClientSession session, PocketType pocket, short slot, short amount, short warehouseSlot)
        {
            var family = session.Character.Family;
            if (family == null || !CanPut(session, family))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotQualifiedForWarehouse });
                return;
            }

            var stored = await transfer.DepositAsync(session, Slots(family), pocket, slot, amount, warehouseSlot,
                item => item.Item.IsTradable && item.BoundCharacterId == null);
            if (stored != null)
            {
                await familyLogService.AddAsync(family.FamilyId, FamilyLogType.WareHouseAdded, session.Character.Name, stored.ItemVNum, amount);
            }
        }

        public async Task WithdrawAsync(ClientSession session, short warehouseSlot, short amount)
        {
            var family = session.Character.Family;
            if (family == null || !CanTake(session, family))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotQualifiedForWarehouse });
                return;
            }

            var taken = await transfer.WithdrawAsync(session, Slots(family), warehouseSlot, amount);
            if (taken != null)
            {
                await familyLogService.AddAsync(family.FamilyId, FamilyLogType.WareHouseRemoved, session.Character.Name, taken.ItemVNum, amount);
            }
        }

        public async Task MoveAsync(ClientSession session, short sourceSlot, short amount, short destinationSlot)
        {
            var family = session.Character.Family;
            if (family == null || !CanTake(session, family))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotQualifiedForWarehouse });
                return;
            }

            await transfer.MoveAsync(session, Slots(family), sourceSlot, amount, destinationSlot);
        }

        private static WarehouseSlots Slots(Family family) =>
            new(family.FamilyId, WarehouseType.FamilyWareHouse, family.WarehouseSize);

        private static bool CanPut(ClientSession session, Family family) => family.AuthorityOf(session.Character.CharacterId) switch
        {
            FamilyAuthority.Head or FamilyAuthority.Assistant => true,
            FamilyAuthority.Manager => family.ManagerAuthorityType != FamilyAuthorityType.NONE,
            _ => family.MemberAuthorityType != FamilyAuthorityType.NONE
        };

        private static bool CanTake(ClientSession session, Family family) => family.AuthorityOf(session.Character.CharacterId) switch
        {
            FamilyAuthority.Head or FamilyAuthority.Assistant => true,
            FamilyAuthority.Manager => family.ManagerAuthorityType == FamilyAuthorityType.ALL,
            _ => family.MemberAuthorityType == FamilyAuthorityType.ALL
        };

        private static FslogStcPacket GenerateFslog(IEnumerable<FamilyLogDto> history, Instant now) => new()
        {
            Entries = history.Select(s => (FslogStcSubPacket?)new FslogStcSubPacket
            {
                Type = s.FamilyLogType == FamilyLogType.WareHouseAdded ? FamilyWarehouseLogType.Deposit : FamilyWarehouseLogType.Withdrawal,
                CharacterName = s.FirstArgument,
                ItemVNum = ParseShort(s.SecondArgument),
                Amount = ParseShort(s.ThirdArgument),
                HoursAgo = (int)(now - s.Timestamp).TotalHours
            }).ToList()
        };

        private static short ParseShort(string? value) =>
            short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : default;
    }
}
