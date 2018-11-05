//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
        private static GenericDao<Account, AccountDto> _accountDao;
        private static GenericDao<Character, CharacterDto> _characterDao;
        private static GenericDao<Map, MapDto> _mapDao;
        private static GenericDao<MapNpc, MapNpcDto> _mapNpcDao;
        private static GenericDao<NpcMonster, NpcMonsterDto> _npcMonsterDao;
        private static GenericDao<Card, CardDto> _cardDao;
        private static GenericDao<Drop, DropDto> _dropDao;
        private static GenericDao<BCard, BCardDto> _bcardDao;
        private static GenericDao<Item, ItemDto> _itemDao;
        private static GenericDao<Quest, QuestDto> _questDao;
        private static GenericDao<QuestReward, QuestRewardDto> _questRewardDao;
        private static GenericDao<QuestObjective, QuestObjectiveDto> _questObjectiveDao;
        private static GenericDao<Mate, MateDto> _mateDao;
        private static GenericDao<Portal, PortalDto> _portalDao;
        private static GenericDao<MapType, MapTypeDto> _mapTypeDao;
        private static GenericDao<Combo, ComboDto> _comboDao;
        private static GenericDao<BCard, BCardDto> _bCardDao;
        private static GenericDao<RespawnMapType, RespawnMapTypeDto> _respawnMapTypeDao;
        private static GenericDao<MapTypeMap, MapTypeMapDto> _mapTypeMapDao;
        private static GenericDao<I18NActDesc, I18NActDescDto> _i18NActDescDao;
        private static GenericDao<I18NCard, I18NCardDto> _i18NCardDao;
        private static GenericDao<I18NBCard, I18NBCardDto> _i18NBCardDao;
        private static GenericDao<I18NItem, I18NItemDto> _i18NItemDao;
        private static GenericDao<I18NMapIdData, I18NMapIdDataDto> _i18NMapIdDataDao;
        private static GenericDao<I18NMapPointData, I18NMapPointDataDto> _i18NMapPointDataDao;
        private static GenericDao<I18NNpcMonster, I18NNpcMonsterDto> _i18NNpcMonsterDao;
        private static GenericDao<I18NNpcMonsterTalk, I18NNpcMonsterTalkDto> _i18NNpcMonsterTalkDao;
        private static GenericDao<I18NQuest, I18NQuestDto> _i18NQuestDao;
        private static GenericDao<I18NSkill, I18NSkillDto> _iI18NSkillDao;
        private static GenericDao<Skill, SkillDto> _skillDao;
        private static GenericDao<NpcMonsterSkill, NpcMonsterSkillDto> _npcMonsterSkillDao;
        private static GenericDao<MapMonster, MapMonsterDto> _mapMonsterDao;
        private static GenericDao<CharacterRelation, CharacterRelationDto> _characterRelationDao;
        private static ItemInstanceDao _itemInstanceDao;

        public static GenericDao<Drop, DropDto> DropDao => _dropDao ??
            (_dropDao = new GenericDao<Drop, DropDto>());

        public static GenericDao<RespawnMapType, RespawnMapTypeDto> RespawnMapTypeDao => _respawnMapTypeDao ??
            (_respawnMapTypeDao = new GenericDao<RespawnMapType, RespawnMapTypeDto>());

        public static GenericDao<Combo, ComboDto> ComboDao => _comboDao ??
            (_comboDao = new GenericDao<Combo, ComboDto>());

        public static GenericDao<BCard, BCardDto> BCardDao => _bCardDao ??
            (_bCardDao = new GenericDao<BCard, BCardDto>());

        public static ItemInstanceDao ItemInstanceDao => _itemInstanceDao ??
            (_itemInstanceDao = new ItemInstanceDao());

        public static GenericDao<Skill, SkillDto> SkillDao => _skillDao ??
            (_skillDao = new GenericDao<Skill, SkillDto>());

        public static GenericDao<NpcMonsterSkill, NpcMonsterSkillDto> NpcMonsterSkillDao => _npcMonsterSkillDao ??
            (_npcMonsterSkillDao = new GenericDao<NpcMonsterSkill, NpcMonsterSkillDto>());

        public static GenericDao<MapType, MapTypeDto> MapTypeDao => _mapTypeDao ??
            (_mapTypeDao = new GenericDao<MapType, MapTypeDto>());

        public static GenericDao<MapTypeMap, MapTypeMapDto> MapTypeMapDao => _mapTypeMapDao ??
            (_mapTypeMapDao = new GenericDao<MapTypeMap, MapTypeMapDto>());

        public static GenericDao<I18NActDesc, I18NActDescDto> I18NActDescDao => _i18NActDescDao ??
            (_i18NActDescDao = new GenericDao<I18NActDesc, I18NActDescDto>());

        public static GenericDao<I18NCard, I18NCardDto> I18NCardDao =>
            _i18NCardDao ?? (_i18NCardDao = new GenericDao<I18NCard, I18NCardDto>());

        public static GenericDao<I18NBCard, I18NBCardDto> I18NbCardDao => _i18NBCardDao ??
            (_i18NBCardDao = new GenericDao<I18NBCard, I18NBCardDto>());

        public static GenericDao<Account, AccountDto> AccountDao =>
            _accountDao ?? (_accountDao = new GenericDao<Account, AccountDto>());

        public static GenericDao<I18NItem, I18NItemDto> I18NItemDao =>
            _i18NItemDao ?? (_i18NItemDao = new GenericDao<I18NItem, I18NItemDto>());

        public static GenericDao<I18NMapIdData, I18NMapIdDataDto> I18NMapIdDataDao => _i18NMapIdDataDao ??
            (_i18NMapIdDataDao = new GenericDao<I18NMapIdData, I18NMapIdDataDto>());

        public static GenericDao<I18NMapPointData, I18NMapPointDataDto> I18NMapPointDataDao =>
            _i18NMapPointDataDao ??
            (_i18NMapPointDataDao = new GenericDao<I18NMapPointData, I18NMapPointDataDto>());

        public static GenericDao<I18NNpcMonster, I18NNpcMonsterDto> I18NNpcMonsterDao => _i18NNpcMonsterDao ??
            (_i18NNpcMonsterDao = new GenericDao<I18NNpcMonster, I18NNpcMonsterDto>());

        public static GenericDao<I18NNpcMonsterTalk, I18NNpcMonsterTalkDto> I18NNpcMonsterTalkDao =>
            _i18NNpcMonsterTalkDao ?? (_i18NNpcMonsterTalkDao =
                new GenericDao<I18NNpcMonsterTalk, I18NNpcMonsterTalkDto>());

        public static GenericDao<I18NQuest, I18NQuestDto> I18NQuestDao => _i18NQuestDao ??
            (_i18NQuestDao = new GenericDao<I18NQuest, I18NQuestDto>());

        public static GenericDao<I18NSkill, I18NSkillDto> I18NSkillDao => _iI18NSkillDao ??
            (_iI18NSkillDao = new GenericDao<I18NSkill, I18NSkillDto>());

        public static GenericDao<Mate, MateDto> MateDao =>
            _mateDao ?? (_mateDao = new GenericDao<Mate, MateDto>());

        public static GenericDao<Character, CharacterDto> CharacterDao =>
            _characterDao ?? (_characterDao = new GenericDao<Character, CharacterDto>());

        public static GenericDao<Map, MapDto> MapDao => _mapDao ?? (_mapDao = new GenericDao<Map, MapDto>());

        public static GenericDao<MapNpc, MapNpcDto> MapNpcDao =>
            _mapNpcDao ?? (_mapNpcDao = new GenericDao<MapNpc, MapNpcDto>());

        public static GenericDao<NpcMonster, NpcMonsterDto> NpcMonsterDao => _npcMonsterDao ??
            (_npcMonsterDao = new GenericDao<NpcMonster, NpcMonsterDto>());

        public static GenericDao<Card, CardDto> CardDao =>
            _cardDao ?? (_cardDao = new GenericDao<Card, CardDto>());

        public static GenericDao<BCard, BCardDto> BcardDao =>
            _bcardDao ?? (_bcardDao = new GenericDao<BCard, BCardDto>());

        public static GenericDao<Item, ItemDto> ItemDao =>
            _itemDao ?? (_itemDao = new GenericDao<Item, ItemDto>());

        public static GenericDao<Quest, QuestDto> QuestDao =>
            _questDao ?? (_questDao = new GenericDao<Quest, QuestDto>());

        public static GenericDao<QuestReward, QuestRewardDto> QuestRewardDao => _questRewardDao ??
            (_questRewardDao = new GenericDao<QuestReward, QuestRewardDto>());

        public static GenericDao<QuestObjective, QuestObjectiveDto> QuestObjectiveDao => _questObjectiveDao ??
            (_questObjectiveDao = new GenericDao<QuestObjective, QuestObjectiveDto>());

        public static GenericDao<Portal, PortalDto> PortalDao =>
            _portalDao ?? (_portalDao = new GenericDao<Portal, PortalDto>());

        public static GenericDao<MapMonster, MapMonsterDto> MapMonsterDao =>
            _mapMonsterDao ?? (_mapMonsterDao = new GenericDao<MapMonster, MapMonsterDto>());

        public static GenericDao<CharacterRelation, CharacterRelationDto> CharacterRelationDao =>
            _characterRelationDao ??
            (_characterRelationDao = new GenericDao<CharacterRelation, CharacterRelationDto>());
    }
}