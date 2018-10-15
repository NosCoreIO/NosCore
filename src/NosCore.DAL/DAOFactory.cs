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
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Database.Entities;

namespace NosCore.DAL
{
    public static class DAOFactory
    {
        private static GenericDAO<Account, AccountDTO> _accountDAO;
        private static GenericDAO<Character, CharacterDTO> _characterDAO;
        private static GenericDAO<Map, MapDTO> _mapDAO;
        private static GenericDAO<MapNpc, MapNpcDTO> _mapNpcDAO;
        private static GenericDAO<NpcMonster, NpcMonsterDTO> _npcMonsterDAO;
        private static GenericDAO<Card, CardDTO> _cardDAO;
        private static GenericDAO<Drop, DropDTO> _dropDAO;
        private static GenericDAO<BCard, BCardDTO> _bcardDAO;
        private static GenericDAO<Item, ItemDTO> _itemDAO;
        private static GenericDAO<Quest, QuestDTO> _questDAO;
        private static GenericDAO<QuestReward, QuestRewardDTO> _questRewardDAO;
        private static GenericDAO<QuestObjective, QuestObjectiveDTO> _questObjectiveDAO;
        private static GenericDAO<Mate, MateDTO> _mateDAO;
        private static GenericDAO<Portal, PortalDTO> _portalDAO;
        private static GenericDAO<Database.Entities.MapType, MapTypeDTO> _mapTypeDAO;
        private static GenericDAO<Combo, ComboDTO> _comboDAO;
        private static GenericDAO<BCard, BCardDTO> _bCardDAO;
        private static GenericDAO<RespawnMapType, RespawnMapTypeDTO> _respawnMapTypeDAO;
        private static GenericDAO<MapTypeMap, MapTypeMapDTO> _mapTypeMapDAO;
        private static GenericDAO<I18N_ActDesc, I18N_ActDescDTO> _i18N_ActDescDAO;
        private static GenericDAO<I18N_Card, I18N_CardDTO> _i18N_CardDAO;
        private static GenericDAO<I18N_BCard, I18N_BCardDTO> _i18N_BCardDAO;
        private static GenericDAO<I18N_Item, I18N_ItemDTO> _i18N_ItemDAO;
        private static GenericDAO<I18N_MapIdData, I18N_MapIdDataDTO> _i18N_MapIdDataDAO;
        private static GenericDAO<I18N_MapPointData, I18N_MapPointDataDTO> _i18N_MapPointDataDAO;
        private static GenericDAO<I18N_NpcMonster, I18N_NpcMonsterDTO> _i18N_NpcMonsterDAO;
        private static GenericDAO<I18N_NpcMonsterTalk, I18N_NpcMonsterTalkDTO> _i18N_NpcMonsterTalkDAO;
        private static GenericDAO<I18N_Quest, I18N_QuestDTO> _i18N_QuestDAO;
        private static GenericDAO<I18N_Skill, I18N_SkillDTO> _iI18N_SkillDAO;
        private static GenericDAO<Skill, SkillDTO> _skillDAO;
        private static GenericDAO<NpcMonsterSkill, NpcMonsterSkillDTO> _npcMonsterSkillDAO;
        private static GenericDAO<MapMonster, MapMonsterDTO> _mapMonsterDAO;
        private static GenericDAO<CharacterRelation, CharacterRelationDTO> _characterRelationDAO;
        private static GenericDAO<ItemInstance, ItemInstanceDTO> _itemInstanceDAO;

        public static GenericDAO<Drop, DropDTO> DropDAO => _dropDAO ??
            (_dropDAO = new GenericDAO<Drop, DropDTO>());

        public static GenericDAO<RespawnMapType, RespawnMapTypeDTO> RespawnMapTypeDAO => _respawnMapTypeDAO ??
            (_respawnMapTypeDAO = new GenericDAO<RespawnMapType, RespawnMapTypeDTO>());

        public static GenericDAO<Combo, ComboDTO> ComboDAO => _comboDAO ??
            (_comboDAO = new GenericDAO<Combo, ComboDTO>());

        public static GenericDAO<BCard, BCardDTO> BCardDAO => _bCardDAO ??
            (_bCardDAO = new GenericDAO<BCard, BCardDTO>());

        public static GenericDAO<ItemInstance, ItemInstanceDTO> ItemInstanceDAO => _itemInstanceDAO ??
            (_itemInstanceDAO = new GenericDAO<ItemInstance, ItemInstanceDTO>());

        public static GenericDAO<Skill, SkillDTO> SkillDAO => _skillDAO ??
            (_skillDAO = new GenericDAO<Skill, SkillDTO>());

        public static GenericDAO<NpcMonsterSkill, NpcMonsterSkillDTO> NpcMonsterSkillDAO => _npcMonsterSkillDAO ??
            (_npcMonsterSkillDAO = new GenericDAO<NpcMonsterSkill, NpcMonsterSkillDTO>());

        public static GenericDAO<Database.Entities.MapType, MapTypeDTO> MapTypeDAO => _mapTypeDAO ??
            (_mapTypeDAO = new GenericDAO<Database.Entities.MapType, MapTypeDTO>());

        public static GenericDAO<MapTypeMap, MapTypeMapDTO> MapTypeMapDAO => _mapTypeMapDAO ??
            (_mapTypeMapDAO = new GenericDAO<MapTypeMap, MapTypeMapDTO>());

        public static GenericDAO<I18N_ActDesc, I18N_ActDescDTO> I18N_ActDescDAO => _i18N_ActDescDAO ??
            (_i18N_ActDescDAO = new GenericDAO<I18N_ActDesc, I18N_ActDescDTO>());

        public static GenericDAO<I18N_Card, I18N_CardDTO> I18N_CardDAO =>
            _i18N_CardDAO ?? (_i18N_CardDAO = new GenericDAO<I18N_Card, I18N_CardDTO>());

        public static GenericDAO<I18N_BCard, I18N_BCardDTO> I18N_BCardDAO => _i18N_BCardDAO ??
            (_i18N_BCardDAO = new GenericDAO<I18N_BCard, I18N_BCardDTO>());

        public static GenericDAO<Account, AccountDTO> AccountDAO =>
            _accountDAO ?? (_accountDAO = new GenericDAO<Account, AccountDTO>());

        public static GenericDAO<I18N_Item, I18N_ItemDTO> I18N_ItemDAO =>
            _i18N_ItemDAO ?? (_i18N_ItemDAO = new GenericDAO<I18N_Item, I18N_ItemDTO>());

        public static GenericDAO<I18N_MapIdData, I18N_MapIdDataDTO> I18N_MapIdDataDAO => _i18N_MapIdDataDAO ??
            (_i18N_MapIdDataDAO = new GenericDAO<I18N_MapIdData, I18N_MapIdDataDTO>());

        public static GenericDAO<I18N_MapPointData, I18N_MapPointDataDTO> I18N_MapPointDataDAO =>
            _i18N_MapPointDataDAO ??
            (_i18N_MapPointDataDAO = new GenericDAO<I18N_MapPointData, I18N_MapPointDataDTO>());

        public static GenericDAO<I18N_NpcMonster, I18N_NpcMonsterDTO> I18N_NpcMonsterDAO => _i18N_NpcMonsterDAO ??
            (_i18N_NpcMonsterDAO = new GenericDAO<I18N_NpcMonster, I18N_NpcMonsterDTO>());

        public static GenericDAO<I18N_NpcMonsterTalk, I18N_NpcMonsterTalkDTO> I18N_NpcMonsterTalkDAO =>
            _i18N_NpcMonsterTalkDAO ?? (_i18N_NpcMonsterTalkDAO =
                new GenericDAO<I18N_NpcMonsterTalk, I18N_NpcMonsterTalkDTO>());

        public static GenericDAO<I18N_Quest, I18N_QuestDTO> I18N_QuestDAO => _i18N_QuestDAO ??
            (_i18N_QuestDAO = new GenericDAO<I18N_Quest, I18N_QuestDTO>());

        public static GenericDAO<I18N_Skill, I18N_SkillDTO> I18N_SkillDAO => _iI18N_SkillDAO ??
            (_iI18N_SkillDAO = new GenericDAO<I18N_Skill, I18N_SkillDTO>());

        public static GenericDAO<Mate, MateDTO> MateDAO =>
            _mateDAO ?? (_mateDAO = new GenericDAO<Mate, MateDTO>());

        public static GenericDAO<Character, CharacterDTO> CharacterDAO =>
            _characterDAO ?? (_characterDAO = new GenericDAO<Character, CharacterDTO>());

        public static GenericDAO<Map, MapDTO> MapDAO => _mapDAO ?? (_mapDAO = new GenericDAO<Map, MapDTO>());

        public static GenericDAO<MapNpc, MapNpcDTO> MapNpcDAO =>
            _mapNpcDAO ?? (_mapNpcDAO = new GenericDAO<MapNpc, MapNpcDTO>());

        public static GenericDAO<NpcMonster, NpcMonsterDTO> NpcMonsterDAO => _npcMonsterDAO ??
            (_npcMonsterDAO = new GenericDAO<NpcMonster, NpcMonsterDTO>());

        public static GenericDAO<Card, CardDTO> CardDAO =>
            _cardDAO ?? (_cardDAO = new GenericDAO<Card, CardDTO>());

        public static GenericDAO<BCard, BCardDTO> BcardDAO =>
            _bcardDAO ?? (_bcardDAO = new GenericDAO<BCard, BCardDTO>());

        public static GenericDAO<Item, ItemDTO> ItemDAO =>
            _itemDAO ?? (_itemDAO = new GenericDAO<Item, ItemDTO>());

        public static GenericDAO<Quest, QuestDTO> QuestDAO =>
            _questDAO ?? (_questDAO = new GenericDAO<Quest, QuestDTO>());

        public static GenericDAO<QuestReward, QuestRewardDTO> QuestRewardDAO => _questRewardDAO ??
            (_questRewardDAO = new GenericDAO<QuestReward, QuestRewardDTO>());

        public static GenericDAO<QuestObjective, QuestObjectiveDTO> QuestObjectiveDAO => _questObjectiveDAO ??
            (_questObjectiveDAO = new GenericDAO<QuestObjective, QuestObjectiveDTO>());

        public static GenericDAO<Portal, PortalDTO> PortalDAO =>
            _portalDAO ?? (_portalDAO = new GenericDAO<Portal, PortalDTO>());

        public static GenericDAO<MapMonster, MapMonsterDTO> MapMonsterDAO =>
            _mapMonsterDAO ?? (_mapMonsterDAO = new GenericDAO<MapMonster, MapMonsterDTO>());

        public static GenericDAO<CharacterRelation, CharacterRelationDTO> CharacterRelationDAO =>
            _characterRelationDAO ?? (_characterRelationDAO = new GenericDAO<CharacterRelation, CharacterRelationDTO>());
    }
}