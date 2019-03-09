//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using Mapster;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Database.Entities;
using MapType = NosCore.Database.Entities.MapType;

namespace NosCore.DAL
{
    public static class DaoFactory
    {
        private static IGenericDao<AccountDto> _accountDao = new GenericDao<Account, AccountDto>();
        private static IGenericDao<CharacterDto> _characterDao = new GenericDao<Character, CharacterDto>();
        private static IGenericDao<MapDto> _mapDao = new GenericDao<Map, MapDto>();
        private static IGenericDao<MapNpcDto> _mapNpcDao = new GenericDao<MapNpc, MapNpcDto>();
        private static IGenericDao<NpcMonsterDto> _npcMonsterDao = new GenericDao<NpcMonster, NpcMonsterDto>();
        private static IGenericDao<CardDto> _cardDao = new GenericDao<Card, CardDto>();
        private static IGenericDao<DropDto> _dropDao = new GenericDao<Drop, DropDto>();
        private static IGenericDao<BCardDto> _bcardDao = new GenericDao<BCard, BCardDto>();
        private static IGenericDao<ItemDto> _itemDao = new GenericDao<Item, ItemDto>();
        private static IGenericDao<QuestDto> _questDao = new GenericDao<Quest, QuestDto>();
        private static IGenericDao<QuestRewardDto> _questRewardDao = new GenericDao<QuestReward, QuestRewardDto>();

        private static IGenericDao<QuestObjectiveDto> _questObjectiveDao =
            new GenericDao<QuestObjective, QuestObjectiveDto>();

        private static IGenericDao<MateDto> _mateDao = new GenericDao<Mate, MateDto>();
        private static IGenericDao<PortalDto> _portalDao = new GenericDao<Portal, PortalDto>();
        private static IGenericDao<MapTypeDto> _mapTypeDao = new GenericDao<MapType, MapTypeDto>();
        private static IGenericDao<ComboDto> _comboDao = new GenericDao<Combo, ComboDto>();
        private static IGenericDao<BCardDto> _bCardDao = new GenericDao<BCard, BCardDto>();

        private static IGenericDao<RespawnMapTypeDto> _respawnMapTypeDao =
            new GenericDao<RespawnMapType, RespawnMapTypeDto>();

        private static IGenericDao<MapTypeMapDto> _mapTypeMapDao = new GenericDao<MapTypeMap, MapTypeMapDto>();
        private static IGenericDao<I18NActDescDto> _i18NActDescDao = new GenericDao<I18NActDesc, I18NActDescDto>();
        private static IGenericDao<I18NCardDto> _i18NCardDao = new GenericDao<I18NCard, I18NCardDto>();
        private static IGenericDao<I18NbCardDto> _i18NBCardDao = new GenericDao<I18NBCard, I18NbCardDto>();
        private static IGenericDao<I18NItemDto> _i18NItemDao = new GenericDao<I18NItem, I18NItemDto>();

        private static IGenericDao<I18NMapIdDataDto> _i18NMapIdDataDao =
            new GenericDao<I18NMapIdData, I18NMapIdDataDto>();

        private static IGenericDao<I18NMapPointDataDto> _i18NMapPointDataDao =
            new GenericDao<I18NMapPointData, I18NMapPointDataDto>();

        private static IGenericDao<I18NNpcMonsterDto> _i18NNpcMonsterDao =
            new GenericDao<I18NNpcMonster, I18NNpcMonsterDto>();

        private static IGenericDao<I18NNpcMonsterTalkDto> _i18NNpcMonsterTalkDao =
            new GenericDao<I18NNpcMonsterTalk, I18NNpcMonsterTalkDto>();

        private static IGenericDao<I18NQuestDto> _i18NQuestDao = new GenericDao<I18NQuest, I18NQuestDto>();
        private static IGenericDao<I18NSkillDto> _iI18NSkillDao = new GenericDao<I18NSkill, I18NSkillDto>();
        private static IGenericDao<SkillDto> _skillDao = new GenericDao<Skill, SkillDto>();

        private static IGenericDao<NpcMonsterSkillDto> _npcMonsterSkillDao =
            new GenericDao<NpcMonsterSkill, NpcMonsterSkillDto>();

        private static IGenericDao<MapMonsterDto> _mapMonsterDao = new GenericDao<MapMonster, MapMonsterDto>();

        private static IGenericDao<CharacterRelationDto> _characterRelationDao =
            new GenericDao<CharacterRelation, CharacterRelationDto>();

        private static IGenericDao<IItemInstanceDto> _itemInstanceDao = new ItemInstanceDao();
        private static IGenericDao<FamilyDto> _familyDao = new GenericDao<Family, FamilyDto>();

        private static IGenericDao<FamilyCharacterDto> _familyCharacterDao =
            new GenericDao<FamilyCharacter, FamilyCharacterDto>();

        private static IGenericDao<FamilyLogDto> _familyLogDao = new GenericDao<FamilyLog, FamilyLogDto>();
        private static IGenericDao<ShopDto> _shopDao = new GenericDao<Shop, ShopDto>();
        private static IGenericDao<ShopItemDto> _shopItemDao = new GenericDao<ShopItem, ShopItemDto>();

        public static IGenericDao<TEntity> GetGenericDao<TEntity>()
        {
            if (typeof(AccountDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _accountDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(CharacterDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _characterDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(MapDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _mapDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(MapNpcDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _mapNpcDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(NpcMonsterDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _npcMonsterDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(CardDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _cardDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(DropDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _dropDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(BCardDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _bcardDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(ItemDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _itemDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(DropDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _questDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(QuestDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _dropDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(QuestRewardDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _questRewardDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(QuestObjectiveDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _questObjectiveDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(MateDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _mateDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(PortalDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _portalDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(MapTypeDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _mapTypeDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(ComboDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _comboDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(BCardDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _bCardDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(RespawnMapTypeDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _respawnMapTypeDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(MapTypeMapDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _mapTypeMapDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(I18NActDescDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _i18NActDescDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(I18NCardDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _i18NCardDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(I18NbCardDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _i18NBCardDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(I18NItemDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _i18NItemDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(I18NMapIdDataDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _i18NMapIdDataDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(I18NMapPointDataDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _i18NMapPointDataDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(I18NNpcMonsterDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _i18NNpcMonsterDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(I18NNpcMonsterTalkDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _i18NNpcMonsterTalkDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(I18NQuestDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _i18NQuestDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(I18NSkillDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _iI18NSkillDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(SkillDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _skillDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(NpcMonsterSkillDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _npcMonsterSkillDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(MapMonsterDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _mapMonsterDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(CharacterRelationDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _characterRelationDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(IItemInstanceDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _itemInstanceDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(FamilyDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _familyDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(FamilyCharacterDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _familyCharacterDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(FamilyLogDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _familyLogDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(ShopDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _shopDao.Adapt<IGenericDao<TEntity>>();
            }

            if (typeof(ShopItemDto).IsAssignableFrom(typeof(TEntity)))
            {
                return _shopItemDao.Adapt<IGenericDao<TEntity>>();
            }

            throw new ArgumentException();
        }
    }
}