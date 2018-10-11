namespace NosCore.Database.Entities
{
    public class QuestReward
    {
        #region Properties

        public long QuestRewardId { get; set; }

        public byte RewardType { get; set; }

        public int Data { get; set; }

        public byte Design { get; set; }

        public byte Rarity { get; set; }

        public byte Upgrade { get; set; }

        public int Amount { get; set; }

        public short QuestId { get; set; }

        public virtual Quest Quest { get; set; }

        #endregion
    }
}