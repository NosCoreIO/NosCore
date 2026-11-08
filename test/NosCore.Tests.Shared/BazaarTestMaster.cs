//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database.Entities;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Services.BazaarService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.Tests.Shared
{
    // A master-side bazaar on the test database, with real DAOs and transactions.
    public class BazaarTestMaster
    {
        public BazaarTestMaster()
        {
            var context = TestHelpers.Instance.DbContextBuilder;
            ItemInstanceDao = new Dao<ItemInstance, IItemInstanceDto?, Guid>(
                NullLogger<Dao<ItemInstance, IItemInstanceDto?, Guid>>.Instance, context);
            BazaarItemDao = new Dao<BazaarItem, BazaarItemDto, long>(
                NullLogger<Dao<BazaarItem, BazaarItemDto, long>>.Instance, context);
            OperationDao = new Dao<BazaarOperation, BazaarOperationDto, Guid>(
                NullLogger<Dao<BazaarOperation, BazaarOperationDto, Guid>>.Instance, context);
            Registry = new BazaarRegistry(BazaarItemDao, ItemInstanceDao, new Mock<IDao<CharacterDto, long>>().Object,
                OperationDao);
            Service = new BazaarService(Registry, BazaarItemDao, ItemInstanceDao, OperationDao,
                new DaoTransactionScope(context), TestHelpers.Instance.Clock, new List<ItemDto>());
            Hub = new BazaarHub(Service);
        }

        public IDao<IItemInstanceDto?, Guid> ItemInstanceDao { get; }

        public IDao<BazaarItemDto, long> BazaarItemDao { get; }

        public IDao<BazaarOperationDto, Guid> OperationDao { get; }

        public BazaarRegistry Registry { get; }

        public BazaarService Service { get; }

        public BazaarHub Hub { get; }

        public Task<long> ListAsync(long sellerId, string sellerName, short vnum, short amount, long price,
            bool isPackage = false, bool medal = false) =>
            ListAsync(new ItemInstanceDto { Id = Guid.NewGuid(), ItemVNum = vnum, Amount = amount }, sellerId,
                sellerName, price, isPackage, medal);

        public async Task<long> ListAsync(IItemInstanceDto instance, long sellerId, string sellerName, long price,
            bool isPackage = false, bool medal = false)
        {
            var amount = instance.Amount;
            await ItemInstanceDao.TryInsertOrUpdateAsync(instance);
            var operationId = Guid.NewGuid();
            await Hub.AddBazaarAsync(new BazaarRequest
            {
                OperationId = operationId,
                ItemInstanceId = instance.Id,
                CharacterId = sellerId,
                CharacterName = sellerName,
                HasMedal = medal,
                Price = price,
                IsPackage = isPackage,
                Duration = 24,
                Amount = amount
            });
            await Hub.SettleBazaarAsync(operationId);
            var operation = await OperationDao.FirstOrDefaultAsync(s => s.BazaarOperationId == operationId);
            return operation!.BazaarItemId;
        }
    }
}
