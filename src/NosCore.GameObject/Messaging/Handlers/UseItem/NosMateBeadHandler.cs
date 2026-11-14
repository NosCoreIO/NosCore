//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class NosMateBeadHandler(IDao<MateDto, long> mateDao, IMateService mateService,
        IMinilandService minilandService, List<NpcMonsterDto> npcMonsters)
    {
        private const byte PartnerSubType = 1;
        private const short NewMateLoyalty = 1000;

        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var source = evt.InventoryItem;
            var item = source.ItemInstance.Item;
            if (item.ItemType != ItemType.Box || (BoxItemEffectType)item.Effect != BoxItemEffectType.NosMateBead)
            {
                return;
            }

            var session = evt.ClientSession;
            var owner = session.Character;
            var mateType = item.ItemSubType == PartnerSubType ? MateType.Partner : MateType.Pet;
            if (evt.Packet.Mode == 0)
            {
                await session.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = owner.GenerateUseItem(source.Type.ToPocketType(), source.Slot, 1, 0),
                    Question = mateType == MateType.Partner
                        ? Game18NConstString.AskRemovePartnerFromBead
                        : Game18NConstString.AskRemovePetFromBead
                });
                return;
            }

            if (minilandService.GetMinilandFromMapInstanceId(owner.MapInstance.MapInstanceId)?.OwnerId != owner.CharacterId)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.MinilandOnly
                });
                return;
            }

            if (npcMonsters.Find(s => s.NpcMonsterVNum == item.EffectValue) is not { } monster
                || !owner.InventoryService.TryConsume(source, 1))
            {
                return;
            }

            var row = await mateDao.TryInsertOrUpdateAsync(new MateDto
            {
                CharacterId = owner.CharacterId,
                VNum = (short)monster.NpcMonsterVNum,
                MateType = mateType,
                Level = (byte)System.Math.Max(1, (int)item.LevelMinimum),
                Loyalty = NewMateLoyalty,
                Hp = monster.MaxHp,
                Mp = monster.MaxMp,
                IsSummonable = true,
                IsTeamMember = false
            });
            if (row == null)
            {
                owner.InventoryService.RestoreConsumed(source, 1);
                return;
            }

            var mate = mateService.Adopt(row, monster, owner.Mates.Values);
            owner.Mates[mate.MateTransportId] = mate;
            var (x, y) = MatePlacement.MinilandSpot(mate, owner.MapInstance.Map);
            await mate.SpawnAsync(owner, owner.MapInstance, x, y);

            await session.SendPacketAsync(owner.InventoryService.LoadBySlotAndType(source.Slot, source.Type)
                .GeneratePocketChange(source.Type.ToPocketType(), source.Slot));
            await session.SendPacketsAsync(owner.GenerateMateRoster());
            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = mateType == MateType.Partner ? Game18NConstString.PartnerOutOfBead : Game18NConstString.PetOutOfBead
            });
        }
    }
}
