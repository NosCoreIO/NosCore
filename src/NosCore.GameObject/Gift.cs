namespace NosCore.GameObject
{
    public class Gift
    {
        public Gift()
        {
        }

        public Gift(short vnum, byte amount, short design = 0, bool isRareRandom = false)
        {
            VNum = vnum;
            Amount = amount;
            IsRareRandom = isRareRandom;
            Design = design;
        }

        public byte Amount { get; set; }

        public short Design { get; set; }

        public short VNum { get; set; }

        public bool IsRandomRare { get; set; }

        public bool IsRareRandom { get; set; }
    }
}