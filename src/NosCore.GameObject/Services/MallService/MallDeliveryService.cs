//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.Services.MailService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Services.MallService
{
    public class MallDeliveryService(IDao<MallDeliveryDto, Guid> mallDeliveryDao, IMailHub mailHub, IClock clock)
        : IMallDeliveryService
    {
        public async Task<bool> EnqueueAsync(long characterId, short vNum, short amount) =>
            await mallDeliveryDao.TryInsertOrUpdateAsync(new MallDeliveryDto
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                VNum = vNum,
                Amount = amount,
                CreatedAt = clock.GetCurrentInstant()
            }) != null;

        public async Task DeliverPendingAsync(PlayerComponentBundle player)
        {
            var characterId = player.CharacterId;
            var pending = mallDeliveryDao.Where(d => d.CharacterId == characterId && d.DeliveredAt == null)?
                .OrderBy(d => d.CreatedAt).ToList() ?? [];
            foreach (var delivery in pending)
            {
                // Marked before the parcel is sent, so a second sync can never hand the same item out again.
                delivery.DeliveredAt = clock.GetCurrentInstant();
                if (await mallDeliveryDao.TryInsertOrUpdateAsync(delivery) == null)
                {
                    continue;
                }

                if (!await mailHub.SendMailAsync(GiftHelper.GenerateMailRequest(clock, player, characterId, null,
                        delivery.VNum, delivery.Amount, 0, 0, true, null, null)))
                {
                    delivery.DeliveredAt = null;
                    _ = await mallDeliveryDao.TryInsertOrUpdateAsync(delivery);
                    continue;
                }

                await player.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.ThanksPurchaseInNosmall,
                    ArgumentType = 1,
                    Game18NArguments = { delivery.VNum }
                });
            }
        }
    }
}
