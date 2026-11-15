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
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using MateModel = NosCore.GameObject.Services.MateService.Mate;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class NosMateBeadHandler(IDao<MateDto, long> mateDao, IMateService mateService,
        IMinilandService minilandService, List<NpcMonsterDto> npcMonsters,
        IPartnerSkillCatalog partnerSkillCatalog, IRandomProvider random)
    {
        private const byte PartnerSubType = 1;
        private const short NewMateLoyalty = 1000;

        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Box || item.ItemSubType > PartnerSubType)
            {
                return;
            }

            var mateType = item.ItemSubType == PartnerSubType ? MateType.Partner : MateType.Pet;
            switch ((BoxItemEffectType)item.Effect)
            {
                case BoxItemEffectType.NosMateBead:
                    await ReleaseAsync(evt, mateType, (short)item.EffectValue, (byte)System.Math.Max(1, (int)item.LevelMinimum), 0, 0);
                    break;
                case BoxItemEffectType.NosMateHolder when evt.InventoryItem.ItemInstance is BoxInstance { HoldingVNum: > 0 } holder:
                    await ReleaseAsync(evt, mateType, holder.HoldingVNum.Value, System.Math.Max((byte)1, holder.SpLevel),
                        holder.SpDamage, holder.SpDefence);
                    break;
                case BoxItemEffectType.NosMateHolder when evt.InventoryItem.ItemInstance is BoxInstance holder:
                    await StoreAsync(evt, mateType, holder);
                    break;
            }
        }

        private async Task StoreAsync(ItemUsedEvent evt, MateType mateType, BoxInstance holder)
        {
            var source = evt.InventoryItem;
            var owner = evt.ClientSession.Character;
            // The client names the NosMate the player has selected as the target of the item.
            if (evt.Packet.VisualType != VisualType.Npc
                || !owner.Mates.TryGetValue(evt.Packet.VisualId, out var mate)
                || mate.MateType != mateType)
            {
                await SendAsync(owner, Game18NConstString.CanNotPutNosmateInBead);
                return;
            }

            if (mate.EquipmentPocket() is { } pocket && owner.InventoryService.Values.Any(s => s.Type == pocket))
            {
                await SendAsync(owner, Game18NConstString.DisarmPetToBeadHim);
                return;
            }

            if (evt.Packet.Mode == 0)
            {
                await owner.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = new UseItemPacket
                    {
                        VisualType = VisualType.Npc,
                        VisualId = mate.MateTransportId,
                        Type = source.Type.ToPocketType(),
                        Slot = source.Slot,
                        Mode = 1,
                        Parameter = 0
                    },
                    Question = mateType == MateType.Pet ? Game18NConstString.AskPutNosmateInBead
                        : mate.Skin != 0 ? Game18NConstString.AskPartnerInBead
                        : Game18NConstString.AskPutPartnerToBead
                });
                return;
            }

            var (vnum, level, attack, defence) = ((short)mate.VNum, mate.Level, mate.Attack, mate.Defence);
            if (!await mateService.ReleaseAsync(owner, mate))
            {
                return;
            }

            holder.HoldingVNum = vnum;
            holder.SpLevel = level;
            holder.SpDamage = attack;
            holder.SpDefence = defence;

            await owner.SendPacketAsync(source.GeneratePocketChange(source.Type.ToPocketType(), source.Slot));
            await owner.SendPacketsAsync(owner.GenerateMateRoster(partnerSkillCatalog));
            await owner.SendPacketAsync(owner.GeneratePinit());
            await SendAsync(owner, mateType == MateType.Partner ? Game18NConstString.PartnerIsNowInBead : Game18NConstString.PetIsNowInBead);
        }

        private async Task ReleaseAsync(ItemUsedEvent evt, MateType mateType, short monsterVNum, byte level, byte attack, byte defence)
        {
            var source = evt.InventoryItem;
            var session = evt.ClientSession;
            var owner = session.Character;
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
                await SendAsync(owner, Game18NConstString.MinilandOnly);
                return;
            }

            if (npcMonsters.Find(s => s.NpcMonsterVNum == monsterVNum) is not { } monster
                || !owner.InventoryService.TryConsume(source, 1))
            {
                return;
            }

            var row = await mateDao.TryInsertOrUpdateAsync(new MateDto
            {
                CharacterId = owner.CharacterId,
                VNum = (short)monster.NpcMonsterVNum,
                MateType = mateType,
                Level = level,
                Attack = attack,
                Defence = defence,
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
            var (x, y) = MatePlacement.MinilandSpot(mate, owner.MapInstance.Map, random);
            await mate.SpawnAsync(owner, owner.MapInstance, x, y);

            await session.SendPacketAsync(owner.InventoryService.LoadBySlotAndType(source.Slot, source.Type)
                .GeneratePocketChange(source.Type.ToPocketType(), source.Slot));
            await session.SendPacketsAsync(owner.GenerateMateRoster(partnerSkillCatalog));
            await SendAsync(owner, mateType == MateType.Partner ? Game18NConstString.PartnerOutOfBead : Game18NConstString.PetOutOfBead);
        }

        private static Task SendAsync(PlayerComponentBundle owner, Game18NConstString message) =>
            owner.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = message
            });
    }
}
