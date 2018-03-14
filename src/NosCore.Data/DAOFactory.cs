using NosCore.Database;
using NosCore.Database.Entities;

namespace NosCore.Data
{
    public class DAOFactory
    {
        #region Members
        
        private static IGenericDAO<Account,AccountDTO> _accountDAO;
        private static IGenericDAO<Character, CharacterDTO> _characterDAO;
        private static IGenericDAO<Map, MapDTO> _mapDAO;
        private static IGenericDAO<MapNpc, MapNpcDTO> _mapNpcDAO;
        private static IGenericDAO<NpcMonster, NpcMonsterDTO> _npcMonsterDAO;
        private static IGenericDAO<Card, CardDTO> _cardDAO;
        private static IGenericDAO<BCard, BCardDTO> _bcardDAO;
        private static IGenericDAO<Item, ItemDTO> _itemDAO;
        private static IGenericDAO<Quest, QuestDTO> _questDAO;
        private static IGenericDAO<QuestReward, QuestRewardDTO> _questRewardDAO;
        private static IGenericDAO<QuestObjective, QuestObjectiveDTO> _questObjectiveDAO;
        private static IGenericDAO<Mate, MateDTO> _mateDAO;

        #endregion

        #region Properties

        public static IGenericDAO<Account, AccountDTO> AccountDAO
        {
            get { return _accountDAO ?? (_accountDAO = new GenericDAO<Account, AccountDTO>()); }
        }
        public static IGenericDAO<Mate, MateDTO> MateDAO
        {
            get { return _mateDAO ?? (_mateDAO = new GenericDAO<Mate, MateDTO>()); }
        }

        public static IGenericDAO<Character, CharacterDTO> CharacterDAO
        {
            get { return _characterDAO ?? (_characterDAO = new GenericDAO<Character, CharacterDTO>()); }
        }

        public static IGenericDAO<Map, MapDTO> MapDAO
        {
            get { return _mapDAO ?? (_mapDAO = new GenericDAO<Map, MapDTO>()); }
        }

        public static IGenericDAO<MapNpc, MapNpcDTO> MapNpcDAO
        {
            get { return _mapNpcDAO ?? (_mapNpcDAO = new GenericDAO<MapNpc, MapNpcDTO>());  }
        }

        public static IGenericDAO<NpcMonster, NpcMonsterDTO> NpcMonsterDAO
        {
            get { return _npcMonsterDAO ?? (_npcMonsterDAO = new GenericDAO<NpcMonster, NpcMonsterDTO>()); }
        }

        public static IGenericDAO<Card, CardDTO> CardDAO
        {
            get { return _cardDAO ?? (_cardDAO = new GenericDAO<Card, CardDTO>()); }
        }

        public static IGenericDAO<BCard, BCardDTO> BcardDAO
        {
            get { return _bcardDAO ?? (_bcardDAO = new GenericDAO<BCard, BCardDTO>()); }
        }

        public static IGenericDAO<Item, ItemDTO> ItemDAO
        {
            get { return _itemDAO ?? (_itemDAO = new GenericDAO<Item, ItemDTO>()); }
        }

        public static IGenericDAO<Quest, QuestDTO> QuestDAO
        {
            get { return _questDAO ?? (_questDAO = new GenericDAO<Quest, QuestDTO>()); }
        }

        public static IGenericDAO<QuestReward, QuestRewardDTO> QuestRewardDAO
        {
            get { return _questRewardDAO ?? (_questRewardDAO = new GenericDAO<QuestReward, QuestRewardDTO>()); }
        }

        public static IGenericDAO<QuestObjective, QuestObjectiveDTO> QuestObjectiveDAO
        {
            get { return _questObjectiveDAO ?? (_questObjectiveDAO = new GenericDAO<QuestObjective, QuestObjectiveDTO>()); }
        }

        #endregion
    }
}