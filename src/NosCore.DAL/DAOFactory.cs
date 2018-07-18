using System.Linq;
using System.Reflection;
using AutoMapper;
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
        private static GenericDAO<MapType, MapTypeDTO> _mapTypeDAO;
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
        private static IMapper _mapper;

        public static GenericDAO<Drop, DropDTO> DropDAO => _dropDAO ??
            (_dropDAO = new GenericDAO<Drop, DropDTO>(_mapper));

        public static GenericDAO<RespawnMapType, RespawnMapTypeDTO> RespawnMapTypeDAO => _respawnMapTypeDAO ??
            (_respawnMapTypeDAO = new GenericDAO<RespawnMapType, RespawnMapTypeDTO>(_mapper));

        public static GenericDAO<Combo, ComboDTO> ComboDAO => _comboDAO ??
            (_comboDAO = new GenericDAO<Combo, ComboDTO>(_mapper));

        public static GenericDAO<BCard, BCardDTO> BCardDAO => _bCardDAO ??
            (_bCardDAO = new GenericDAO<BCard, BCardDTO>(_mapper));

        public static GenericDAO<Skill, SkillDTO> SkillDAO => _skillDAO ??
            (_skillDAO = new GenericDAO<Skill, SkillDTO>(_mapper));

        public static GenericDAO<NpcMonsterSkill, NpcMonsterSkillDTO> NpcMonsterSkillDAO => _npcMonsterSkillDAO ??
            (_npcMonsterSkillDAO = new GenericDAO<NpcMonsterSkill, NpcMonsterSkillDTO>(_mapper));

        public static GenericDAO<MapType, MapTypeDTO> MapTypeDAO => _mapTypeDAO ??
            (_mapTypeDAO = new GenericDAO<MapType, MapTypeDTO>(_mapper));

        public static GenericDAO<MapTypeMap, MapTypeMapDTO> MapTypeMapDAO => _mapTypeMapDAO ??
            (_mapTypeMapDAO = new GenericDAO<MapTypeMap, MapTypeMapDTO>(_mapper));

        public static GenericDAO<I18N_ActDesc, I18N_ActDescDTO> I18N_ActDescDAO => _i18N_ActDescDAO ??
            (_i18N_ActDescDAO = new GenericDAO<I18N_ActDesc, I18N_ActDescDTO>(_mapper));

        public static GenericDAO<I18N_Card, I18N_CardDTO> I18N_CardDAO =>
            _i18N_CardDAO ?? (_i18N_CardDAO = new GenericDAO<I18N_Card, I18N_CardDTO>(_mapper));

        public static GenericDAO<I18N_BCard, I18N_BCardDTO> I18N_BCardDAO => _i18N_BCardDAO ??
            (_i18N_BCardDAO = new GenericDAO<I18N_BCard, I18N_BCardDTO>(_mapper));

        public static GenericDAO<Account, AccountDTO> AccountDAO =>
            _accountDAO ?? (_accountDAO = new GenericDAO<Account, AccountDTO>(_mapper));

        public static GenericDAO<I18N_Item, I18N_ItemDTO> I18N_ItemDAO =>
            _i18N_ItemDAO ?? (_i18N_ItemDAO = new GenericDAO<I18N_Item, I18N_ItemDTO>(_mapper));

        public static GenericDAO<I18N_MapIdData, I18N_MapIdDataDTO> I18N_MapIdDataDAO => _i18N_MapIdDataDAO ??
            (_i18N_MapIdDataDAO = new GenericDAO<I18N_MapIdData, I18N_MapIdDataDTO>(_mapper));

        public static GenericDAO<I18N_MapPointData, I18N_MapPointDataDTO> I18N_MapPointDataDAO =>
            _i18N_MapPointDataDAO ??
            (_i18N_MapPointDataDAO = new GenericDAO<I18N_MapPointData, I18N_MapPointDataDTO>(_mapper));

        public static GenericDAO<I18N_NpcMonster, I18N_NpcMonsterDTO> I18N_NpcMonsterDAO => _i18N_NpcMonsterDAO ??
            (_i18N_NpcMonsterDAO = new GenericDAO<I18N_NpcMonster, I18N_NpcMonsterDTO>(_mapper));

        public static GenericDAO<I18N_NpcMonsterTalk, I18N_NpcMonsterTalkDTO> I18N_NpcMonsterTalkDAO =>
            _i18N_NpcMonsterTalkDAO ?? (_i18N_NpcMonsterTalkDAO =
                new GenericDAO<I18N_NpcMonsterTalk, I18N_NpcMonsterTalkDTO>(_mapper));

        public static GenericDAO<I18N_Quest, I18N_QuestDTO> I18N_QuestDAO => _i18N_QuestDAO ??
            (_i18N_QuestDAO = new GenericDAO<I18N_Quest, I18N_QuestDTO>(_mapper));

        public static GenericDAO<I18N_Skill, I18N_SkillDTO> I18N_SkillDAO => _iI18N_SkillDAO ??
            (_iI18N_SkillDAO = new GenericDAO<I18N_Skill, I18N_SkillDTO>(_mapper));

        public static GenericDAO<Mate, MateDTO> MateDAO =>
            _mateDAO ?? (_mateDAO = new GenericDAO<Mate, MateDTO>(_mapper));

        public static GenericDAO<Character, CharacterDTO> CharacterDAO =>
            _characterDAO ?? (_characterDAO = new GenericDAO<Character, CharacterDTO>(_mapper));

        public static GenericDAO<Map, MapDTO> MapDAO => _mapDAO ?? (_mapDAO = new GenericDAO<Map, MapDTO>(_mapper));

        public static GenericDAO<MapNpc, MapNpcDTO> MapNpcDAO =>
            _mapNpcDAO ?? (_mapNpcDAO = new GenericDAO<MapNpc, MapNpcDTO>(_mapper));

        public static GenericDAO<NpcMonster, NpcMonsterDTO> NpcMonsterDAO => _npcMonsterDAO ??
            (_npcMonsterDAO = new GenericDAO<NpcMonster, NpcMonsterDTO>(_mapper));

        public static GenericDAO<Card, CardDTO> CardDAO =>
            _cardDAO ?? (_cardDAO = new GenericDAO<Card, CardDTO>(_mapper));

        public static GenericDAO<BCard, BCardDTO> BcardDAO =>
            _bcardDAO ?? (_bcardDAO = new GenericDAO<BCard, BCardDTO>(_mapper));

        public static GenericDAO<Item, ItemDTO> ItemDAO =>
            _itemDAO ?? (_itemDAO = new GenericDAO<Item, ItemDTO>(_mapper));

        public static GenericDAO<Quest, QuestDTO> QuestDAO =>
            _questDAO ?? (_questDAO = new GenericDAO<Quest, QuestDTO>(_mapper));

        public static GenericDAO<QuestReward, QuestRewardDTO> QuestRewardDAO => _questRewardDAO ??
            (_questRewardDAO = new GenericDAO<QuestReward, QuestRewardDTO>(_mapper));

        public static GenericDAO<QuestObjective, QuestObjectiveDTO> QuestObjectiveDAO => _questObjectiveDAO ??
            (_questObjectiveDAO = new GenericDAO<QuestObjective, QuestObjectiveDTO>(_mapper));

        public static GenericDAO<Portal, PortalDTO> PortalDAO =>
            _portalDAO ?? (_portalDAO = new GenericDAO<Portal, PortalDTO>(_mapper));

        public static GenericDAO<MapMonster, MapMonsterDTO> MapMonsterDAO =>
            _mapMonsterDAO ?? (_mapMonsterDAO = new GenericDAO<MapMonster, MapMonsterDTO>(_mapper));

        public static void RegisterMapping(Assembly gameobjectAssembly)
        {
            var config = new MapperConfiguration(cfg =>
            {
                foreach (var type in typeof(CharacterDTO).Assembly.GetTypes()
                    .Where(t => typeof(IDTO).IsAssignableFrom(t)))
                {
                    var index = type.Name.LastIndexOf("DTO");
                    if (index >= 0)
                    {
                        var name = type.Name.Substring(0, index);
                        var typefound = gameobjectAssembly.GetTypes().SingleOrDefault(t => t.Name.Equals(name));
                        var entitytypefound = typeof(Account).Assembly.GetTypes()
                            .SingleOrDefault(t => t.Name.Equals(name));
                        if (entitytypefound != null)
                        {
                            cfg.CreateMap(type, entitytypefound).ReverseMap();
                            if (typefound != null)
                            {
                                cfg.CreateMap(entitytypefound, type).As(typefound);
                            }
                        }
                    }
                }
            });

            _mapper = config.CreateMapper();
        }
    }
}