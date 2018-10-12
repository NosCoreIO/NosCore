using System;
using NosCore.Shared.Enumerations.Character;

namespace NosCore.GameObject.Helper
{
    public sealed class CharacterHelper
    {
        private static CharacterHelper _instance;

        #region Instantiation

        private CharacterHelper()
        {
            LoadSpeedData();
            LoadJobXpData();
            LoadSpxpData();
            LoadHeroXpData();
            LoadXpData();
            LoadHpData();
            LoadMpData();
            LoadStats();
            LoadHpHealth();
            LoadMpHealth();
            LoadHpHealthStand();
            LoadMpHealthStand();
        }

        #endregion

        public static CharacterHelper Instance => _instance ?? (_instance = new CharacterHelper());

        #region Members

        private int[,] _criticalDist;
        private int[,] _criticalDistRate;
        private int[,] _criticalHit;
        private int[,] _criticalHitRate;
        private int[,] _distDef;
        private int[,] _distDodge;
        private int[,] _distRate;
        private int[,] _hitDef;
        private int[,] _hitDodge;
        private int[,] _hitRate;
        private int[,] _magicalDef;
        private int[,] _maxDist;
        private int[,] _maxHit;
        private int[,] _minDist;

        // difference between class
        private int[,] _minHit;

        // STAT DATA

        // same for all class

        #endregion

        #region Properties

        public double[] FirstJobXpData { get; private set; }

        public double[] HeroXpData { get; private set; }

        public int[,] HpData { get; private set; }

        public int[] HpHealth { get; private set; }

        public int[] HpHealthStand { get; private set; }

        public int[,] MpData { get; private set; }

        public int[] MpHealth { get; private set; }

        public int[] MpHealthStand { get; private set; }

        public double[] SecondJobXpData { get; private set; }

        public byte[] SpeedData { get; private set; }

        public double[] SpxpData { get; private set; }

        public double[] XpData { get; private set; }

        #endregion

        #region Methods

        public static float ExperiencePenalty(byte playerLevel, byte monsterLevel)
        {
            var leveldifference = playerLevel - monsterLevel;
            float penalty;

            // penalty calculation
            switch (leveldifference)
            {
                case 6:
                    penalty = 0.9f;
                    break;

                case 7:
                    penalty = 0.7f;
                    break;

                case 8:
                    penalty = 0.5f;
                    break;

                case 9:
                    penalty = 0.3f;
                    break;

                default:
                    if (leveldifference > 9)
                    {
                        penalty = 0.1f;
                    }
                    else if (leveldifference > 18)
                    {
                        penalty = 0.05f;
                    }
                    else
                    {
                        penalty = 1f;
                    }

                    break;
            }

            return penalty;
        }

        public static float GoldPenalty(byte playerLevel, byte monsterLevel)
        {
            var leveldifference = playerLevel - monsterLevel;
            float penalty;

            // penalty calculation
            switch (leveldifference)
            {
                case 5:
                    penalty = 0.9f;
                    break;

                case 6:
                    penalty = 0.7f;
                    break;

                case 7:
                    penalty = 0.5f;
                    break;

                case 8:
                    penalty = 0.3f;
                    break;

                case 9:
                    penalty = 0.2f;
                    break;

                default:
                    if (leveldifference > 9 && leveldifference < 19)
                    {
                        penalty = 0.1f;
                    }
                    else if (leveldifference > 18 && leveldifference < 30)
                    {
                        penalty = 0.05f;
                    }
                    else if (leveldifference > 30)
                    {
                        penalty = 0f;
                    }
                    else
                    {
                        penalty = 1f;
                    }

                    break;
            }

            return penalty;
        }

        public static long LoadFairyXpData(long elementRate)
        {
            if (elementRate < 40)
            {
                return (elementRate * elementRate) + 50;
            }

            return (elementRate * elementRate * 3) + 50;
        }

        public static int LoadFamilyXpData(byte familyLevel)
        {
            switch (familyLevel)
            {
                case 1:
                    return 100000;

                case 2:
                    return 250000;

                case 3:
                    return 370000;

                case 4:
                    return 560000;

                case 5:
                    return 840000;

                case 6:
                    return 1260000;

                case 7:
                    return 1900000;

                case 8:
                    return 2850000;

                case 9:
                    return 3570000;

                case 10:
                    return 3830000;

                case 11:
                    return 4150000;

                case 12:
                    return 4750000;

                case 13:
                    return 5500000;

                case 14:
                    return 6500000;

                case 15:
                    return 7000000;

                case 16:
                    return 8500000;

                case 17:
                    return 9500000;

                case 18:
                    return 10000000;

                case 19:
                    return 17000000;

                default:
                    return 999999999;
            }
        }

        public int MagicalDefence(CharacterClassType @class, byte level)
        {
            return _magicalDef[(byte) @class, level];
        }

        public int MaxDistance(CharacterClassType @class, byte level)
        {
            return _maxDist[(byte) @class, level];
        }

        public int MaxHit(CharacterClassType @class, byte level)
        {
            return _maxHit[(byte) @class, level];
        }

        public int MinDistance(CharacterClassType @class, byte level)
        {
            return _minDist[(byte) @class, level];
        }

        public int MinHit(CharacterClassType @class, byte level)
        {
            return _minHit[(int) @class, level];
        }

        public int RarityPoint(short rarity, short lvl)
        {
            int p;
            switch (rarity)
            {
                case 0:
                    p = 0;
                    break;

                case 1:
                    p = 1;
                    break;

                case 2:
                    p = 2;
                    break;

                case 3:
                    p = 3;
                    break;

                case 4:
                    p = 4;
                    break;

                case 5:
                    p = 5;
                    break;

                case 6:
                    p = 7;
                    break;

                case 7:
                    p = 10;
                    break;

                case 8:
                    p = 15;
                    break;

                default:
                    p = rarity * 2;
                    break;
            }

            return p * ((lvl / 5) + 1);
        }

        public int SlPoint(short spPoint, short mode)
        {
            try
            {
                var point = 0;
                switch (mode)
                {
                    case 0:
                        if (spPoint <= 10)
                        {
                            point = spPoint;
                        }
                        else if (spPoint <= 28)
                        {
                            point = 10 + ((spPoint - 10) / 2);
                        }
                        else if (spPoint <= 88)
                        {
                            point = 19 + ((spPoint - 28) / 3);
                        }
                        else if (spPoint <= 168)
                        {
                            point = 39 + ((spPoint - 88) / 4);
                        }
                        else if (spPoint <= 268)
                        {
                            point = 59 + ((spPoint - 168) / 5);
                        }
                        else if (spPoint <= 334)
                        {
                            point = 79 + ((spPoint - 268) / 6);
                        }
                        else if (spPoint <= 383)
                        {
                            point = 90 + ((spPoint - 334) / 7);
                        }
                        else if (spPoint <= 391)
                        {
                            point = 97 + ((spPoint - 383) / 8);
                        }
                        else if (spPoint <= 400)
                        {
                            point = 98 + ((spPoint - 391) / 9);
                        }
                        else if (spPoint <= 410)
                        {
                            point = 99 + ((spPoint - 400) / 10);
                        }

                        break;

                    case 2:
                        if (spPoint <= 20)
                        {
                            point = spPoint;
                        }
                        else if (spPoint <= 40)
                        {
                            point = 20 + ((spPoint - 20) / 2);
                        }
                        else if (spPoint <= 70)
                        {
                            point = 30 + ((spPoint - 40) / 3);
                        }
                        else if (spPoint <= 110)
                        {
                            point = 40 + ((spPoint - 70) / 4);
                        }
                        else if (spPoint <= 210)
                        {
                            point = 50 + ((spPoint - 110) / 5);
                        }
                        else if (spPoint <= 270)
                        {
                            point = 70 + ((spPoint - 210) / 6);
                        }
                        else if (spPoint <= 410)
                        {
                            point = 80 + ((spPoint - 270) / 7);
                        }

                        break;

                    case 1:
                        if (spPoint <= 10)
                        {
                            point = spPoint;
                        }
                        else if (spPoint <= 48)
                        {
                            point = 10 + ((spPoint - 10) / 2);
                        }
                        else if (spPoint <= 81)
                        {
                            point = 29 + ((spPoint - 48) / 3);
                        }
                        else if (spPoint <= 161)
                        {
                            point = 40 + ((spPoint - 81) / 4);
                        }
                        else if (spPoint <= 236)
                        {
                            point = 60 + ((spPoint - 161) / 5);
                        }
                        else if (spPoint <= 290)
                        {
                            point = 75 + ((spPoint - 236) / 6);
                        }
                        else if (spPoint <= 360)
                        {
                            point = 84 + ((spPoint - 290) / 7);
                        }
                        else if (spPoint <= 400)
                        {
                            point = 97 + ((spPoint - 360) / 8);
                        }
                        else if (spPoint <= 410)
                        {
                            point = 99 + ((spPoint - 400) / 10);
                        }

                        break;

                    case 3:
                        if (spPoint <= 10)
                        {
                            point = spPoint;
                        }
                        else if (spPoint <= 50)
                        {
                            point = 10 + ((spPoint - 10) / 2);
                        }
                        else if (spPoint <= 110)
                        {
                            point = 30 + ((spPoint - 50) / 3);
                        }
                        else if (spPoint <= 150)
                        {
                            point = 50 + ((spPoint - 110) / 4);
                        }
                        else if (spPoint <= 200)
                        {
                            point = 60 + ((spPoint - 150) / 5);
                        }
                        else if (spPoint <= 260)
                        {
                            point = 70 + ((spPoint - 200) / 6);
                        }
                        else if (spPoint <= 330)
                        {
                            point = 80 + ((spPoint - 260) / 7);
                        }
                        else if (spPoint <= 410)
                        {
                            point = 90 + ((spPoint - 330) / 8);
                        }

                        break;
                }

                return point;
            }
            catch
            {
                return 0;
            }
        }

        public int SpPoint(short spLevel, short upgrade)
        {
            var point = spLevel <= 20 ? 0 : (spLevel - 20) * 3;
            switch (upgrade)
            {
                case 1:
                   return point +  5;
                case 2:
                   return point +  10;
                case 3:
                   return point +  15;
                case 4:
                   return point +  20;
                case 5:
                   return point +  28;
                case 6:
                   return point +  36;
                case 7:
                   return point +  46;
                case 8:
                   return point +  56;
                case 9:
                   return point +  68;
                case 10:
                   return point +  80;
                case 11:
                   return point +  95;
                case 12:
                   return point +  110;
                case 13:
                   return point +  128;
                case 14:
                   return point +  148;
                case 15:
                   return point +  173;
                default:
                    return upgrade > 15 ? point + 173 + 25 + (5 * (upgrade - 15)) : point;
            }
        }

        internal int DarkResistance(CharacterClassType @class, byte level)
        {
            return 0;
        }

        internal int Defence(CharacterClassType @class, byte level)
        {
            return _hitDef[(byte) @class, level];
        }

        /// <summary>
        ///     Defence rate base stats for Character by Class & Level
        /// </summary>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        internal int DefenceRate(CharacterClassType @class, byte level)
        {
            return _hitDodge[(byte) @class, level];
        }

        /// <summary>
        ///     Distance Defence base stats for Character by Class & Level
        /// </summary>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        internal int DistanceDefence(CharacterClassType @class, byte level)
        {
            return _distDef[(byte) @class, level];
        }

        /// <summary>
        ///     Distance Defence Rate base stats for Character by Class & Level
        /// </summary>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        internal int DistanceDefenceRate(CharacterClassType @class, byte level)
        {
            return _distDodge[(byte) @class, level];
        }

        /// <summary>
        ///     Distance Rate base stats for Character by Class & Level
        /// </summary>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        internal int DistanceRate(CharacterClassType @class, byte level)
        {
            return _distRate[(byte) @class, level];
        }

        internal int DistCritical(CharacterClassType @class, byte level)
        {
            return _criticalDist[(byte) @class, level];
        }

        internal int DistCriticalRate(CharacterClassType @class, byte level)
        {
            return _criticalDistRate[(byte) @class, level];
        }

        internal int Element(CharacterClassType @class, byte level)
        {
            return 0;
        }

        internal int ElementRate(CharacterClassType @class, byte level)
        {
            return 0;
        }

        internal int FireResistance(CharacterClassType @class, byte level)
        {
            return 0;
        }

        internal int HitCritical(CharacterClassType @class, byte level)
        {
            return _criticalHit[(byte) @class, level];
        }

        internal int HitCriticalRate(CharacterClassType @class, byte level)
        {
            return _criticalHitRate[(byte) @class, level];
        }

        internal int HitRate(CharacterClassType @class, byte level)
        {
            return _hitRate[(byte) @class, level];
        }

        internal int LightResistance(CharacterClassType @class, byte level)
        {
            return 0;
        }

        internal int WaterResistance(CharacterClassType @class, byte level)
        {
            return 0;
        }

        private void LoadHeroXpData()
        {
            var index = 1;
            var increment = 118980;
            var increment2 = 9120;
            var increment3 = 360;

            HeroXpData = new double[256];
            HeroXpData[0] = 949560;
            for (var lvl = 1; lvl < HeroXpData.Length; lvl++)
            {
                HeroXpData[lvl] = HeroXpData[lvl - 1] + increment;
                increment2 += increment3;
                increment += increment2;
                index++;
                if (index % 10 == 0)
                {
                    if (index / 10 < 3)
                    {
                        increment3 -= index / 10 * 30;
                    }
                    else
                    {
                        increment3 -= 30;
                    }
                }
            }
        }

        private void LoadHpData()
        {
            HpData = new int[4, 256];

            // Adventurer HP
            for (var i = 1; i < HpData.GetLength(1); i++)
            {
                HpData[(int) CharacterClassType.Adventurer, i] = (int) ((1 / 2.0 * i * i) + (31 / 2.0 * i) + 205);
            }

            // Swordsman HP
            for (var i = 0; i < HpData.GetLength(1); i++)
            {
                var j = 16;
                var hp = 946;
                var inc = 85;
                while (j <= i)
                {
                    if (j % 5 == 2)
                    {
                        hp += inc / 2;
                        inc += 2;
                    }
                    else
                    {
                        hp += inc;
                        inc += 4;
                    }

                    ++j;
                }

                HpData[(int) CharacterClassType.Swordman, i] = hp;
            }

            // Magician HP
            for (var i = 0; i < HpData.GetLength(1); i++)
            {
                HpData[(int) CharacterClassType.Magician, i] = (int) (((i + 15) * (i + 15)) + i + 15.0 - 465 + 550);
            }

            // Archer HP
            for (var i = 0; i < HpData.GetLength(1); i++)
            {
                var hp = 680;
                var inc = 35;
                var j = 16;
                while (j <= i)
                {
                    hp += inc;
                    ++inc;
                    if (j % 10 == 1 || j % 10 == 5 || j % 10 == 8)
                    {
                        hp += inc;
                        ++inc;
                    }

                    ++j;
                }

                HpData[(int) CharacterClassType.Archer, i] = hp;
            }
        }

        private void LoadHpHealth()
        {
            HpHealth = new int[4];
            HpHealth[(int) CharacterClassType.Archer] = 60;
            HpHealth[(int) CharacterClassType.Adventurer] = 30;
            HpHealth[(int) CharacterClassType.Swordman] = 90;
            HpHealth[(int) CharacterClassType.Magician] = 30;
        }

        private void LoadHpHealthStand()
        {
            HpHealthStand = new int[4];
            HpHealthStand[(int) CharacterClassType.Archer] = 32;
            HpHealthStand[(int) CharacterClassType.Adventurer] = 25;
            HpHealthStand[(int) CharacterClassType.Swordman] = 26;
            HpHealthStand[(int) CharacterClassType.Magician] = 20;
        }

        private void LoadJobXpData()
        {
            // Load JobData
            FirstJobXpData = new double[21];
            SecondJobXpData = new double[256];
            FirstJobXpData[0] = 2200;
            SecondJobXpData[0] = 17600;
            for (var i = 1; i < FirstJobXpData.Length; i++)
            {
                FirstJobXpData[i] = FirstJobXpData[i - 1] + 700;
            }

            for (var i = 1; i < SecondJobXpData.Length; i++)
            {
                var var2 = 400;
                if (i > 3)
                {
                    var2 = 4500;
                }

                if (i > 40)
                {
                    var2 = 15000;
                }

                SecondJobXpData[i] = SecondJobXpData[i - 1] + var2;
            }
        }

        private void LoadMpData()
        {
            MpData = new int[4, 257];

            // ADVENTURER MP
            MpData[(int) CharacterClassType.Adventurer, 0] = 60;
            var baseAdventurer = 9;
            for (var i = 1; i < MpData.GetLength(1); i += 4)
            {
                MpData[(int) CharacterClassType.Adventurer, i] =
                    MpData[(int) CharacterClassType.Adventurer, i - 1] + baseAdventurer;
                MpData[(int) CharacterClassType.Adventurer, i + 1] =
                    MpData[(int) CharacterClassType.Adventurer, i] + baseAdventurer;
                MpData[(int) CharacterClassType.Adventurer, i + 2] =
                    MpData[(int) CharacterClassType.Adventurer, i + 1] + baseAdventurer;
                baseAdventurer++;
                MpData[(int) CharacterClassType.Adventurer, i + 3] =
                    MpData[(int) CharacterClassType.Adventurer, i + 2] + baseAdventurer;
                baseAdventurer++;
            }

            // SWORDSMAN MP
            for (var i = 1; i < MpData.GetLength(1) - 1; i++)
            {
                MpData[(int) CharacterClassType.Swordman, i] = MpData[(int) CharacterClassType.Adventurer, i];
            }

            // ARCHER MP
            for (var i = 0; i < MpData.GetLength(1) - 1; i++)
            {
                MpData[(int) CharacterClassType.Archer, i] = MpData[(int) CharacterClassType.Adventurer, i + 1];
            }

            // MAGICIAN MP
            for (var i = 0; i < MpData.GetLength(1) - 1; i++)
            {
                MpData[(int) CharacterClassType.Magician, i] = 3 * MpData[(int) CharacterClassType.Adventurer, i];
            }
        }

        private void LoadMpHealth()
        {
            MpHealth = new int[4];
            MpHealth[(int) CharacterClassType.Adventurer] = 10;
            MpHealth[(int) CharacterClassType.Swordman] = 30;
            MpHealth[(int) CharacterClassType.Archer] = 50;
            MpHealth[(int) CharacterClassType.Magician] = 80;
        }

        private void LoadMpHealthStand()
        {
            MpHealthStand = new int[4];
            MpHealthStand[(int) CharacterClassType.Adventurer] = 5;
            MpHealthStand[(int) CharacterClassType.Swordman] = 16;
            MpHealthStand[(int) CharacterClassType.Archer] = 28;
            MpHealthStand[(int) CharacterClassType.Magician] = 40;
        }

        private void LoadSpeedData()
        {
            SpeedData = new byte[4];
            SpeedData[(int) CharacterClassType.Adventurer] = 11;
            SpeedData[(int) CharacterClassType.Swordman] = 11;
            SpeedData[(int) CharacterClassType.Archer] = 12;
            SpeedData[(int) CharacterClassType.Magician] = 10;
        }

        private void LoadSpxpData()
        {
            // Load SpData
            SpxpData = new double[256];
            SpxpData[0] = 15000;
            SpxpData[19] = 218000;
            for (var i = 1; i < 19; i++)
            {
                SpxpData[i] = SpxpData[i - 1] + 10000;
            }

            for (var i = 20; i < SpxpData.Length; i++)
            {
                SpxpData[i] = SpxpData[i - 1] + (6 * ((3 * i * (i + 1)) + 1));
            }
        }

        // TODO: Change or Verify
        private void LoadStats()
        {
            _minHit = new int[4, 256];
            _maxHit = new int[4, 256];
            _hitRate = new int[4, 256];
            _criticalHitRate = new int[4, 256];
            _criticalHit = new int[4, 256];
            _minDist = new int[4, 256];
            _maxDist = new int[4, 256];
            _distRate = new int[4, 256];
            _criticalDistRate = new int[4, 256];
            _criticalDist = new int[4, 256];
            _hitDef = new int[4, 256];
            _hitDodge = new int[4, 256];
            _distDef = new int[4, 256];
            _distDodge = new int[4, 256];
            _magicalDef = new int[4, 256];

            for (var i = 0; i < 256; i++)
            {
                // ADVENTURER
                _minHit[(int) CharacterClassType.Adventurer, i] = i + 9; // approx
                _maxHit[(int) CharacterClassType.Adventurer, i] = i + 9; // approx
                _hitRate[(int) CharacterClassType.Adventurer, i] = i + 9; // approx
                _criticalHitRate[(int) CharacterClassType.Adventurer, i] = 0; // sure
                _criticalHit[(int) CharacterClassType.Adventurer, i] = 0; // sure
                _minDist[(int) CharacterClassType.Adventurer, i] = i + 9; // approx
                _maxDist[(int) CharacterClassType.Adventurer, i] = i + 9; // approx
                _distRate[(int) CharacterClassType.Adventurer, i] = (i + 9) * 2; // approx
                _criticalDistRate[(int) CharacterClassType.Adventurer, i] = 0; // sure
                _criticalDist[(int) CharacterClassType.Adventurer, i] = 0; // sure
                _hitDef[(int) CharacterClassType.Adventurer, i] = i + (9 / 2); // approx
                _hitDodge[(int) CharacterClassType.Adventurer, i] = i + 9; // approx
                _distDef[(int) CharacterClassType.Adventurer, i] = (i + 9) / 2; // approx
                _distDodge[(int) CharacterClassType.Adventurer, i] = i + 9; // approx
                _magicalDef[(int) CharacterClassType.Adventurer, i] = (i + 9) / 2; // approx

                // SWORDMAN
                _criticalHitRate[(int) CharacterClassType.Swordman, i] = 0; // approx
                _criticalHit[(int) CharacterClassType.Swordman, i] = 0; // approx
                _criticalDist[(int) CharacterClassType.Swordman, i] = 0; // approx
                _criticalDistRate[(int) CharacterClassType.Swordman, i] = 0; // approx
                _minDist[(int) CharacterClassType.Swordman, i] = i + 12; // approx
                _maxDist[(int) CharacterClassType.Swordman, i] = i + 12; // approx
                _distRate[(int) CharacterClassType.Swordman, i] = 2 * (i + 12); // approx
                _hitDodge[(int) CharacterClassType.Swordman, i] = i + 12; // approx
                _distDodge[(int) CharacterClassType.Swordman, i] = i + 12; // approx
                _magicalDef[(int) CharacterClassType.Swordman, i] = (i + 9) / 2; // approx
                _hitRate[(int) CharacterClassType.Swordman, i] = i + 27; // approx
                _hitDef[(int) CharacterClassType.Swordman, i] = i + 2; // approx

                _minHit[(int) CharacterClassType.Swordman, i] = (2 * i) + 5; // approx Numbers n such that 10n+9 is prime.
                _maxHit[(int) CharacterClassType.Swordman, i] = (2 * i) + 5; // approx Numbers n such that 10n+9 is prime.
                _distDef[(int) CharacterClassType.Swordman, i] = i; // approx

                // MAGICIAN
                _hitRate[(int) CharacterClassType.Magician, i] = 0; // sure
                _criticalHitRate[(int) CharacterClassType.Magician, i] = 0; // sure
                _criticalHit[(int) CharacterClassType.Magician, i] = 0; // sure
                _criticalDistRate[(int) CharacterClassType.Magician, i] = 0; // sure
                _criticalDist[(int) CharacterClassType.Magician, i] = 0; // sure

                _minDist[(int) CharacterClassType.Magician, i] = 14 + i; // approx
                _maxDist[(int) CharacterClassType.Magician, i] = 14 + i; // approx
                _distRate[(int) CharacterClassType.Magician, i] = (14 + i) * 2; // approx
                _hitDef[(int) CharacterClassType.Magician, i] = (i + 11) / 2; // approx
                _magicalDef[(int) CharacterClassType.Magician, i] = i + 4; // approx
                _hitDodge[(int) CharacterClassType.Magician, i] = 24 + i; // approx
                _distDodge[(int) CharacterClassType.Magician, i] = 14 + i; // approx

                _minHit[(int) CharacterClassType.Magician, i] =
                    (2 * i) + 9; // approx Numbers n such that n^2 is of form x^ 2 + 40y ^ 2 with positive x,y.
                _maxHit[(int) CharacterClassType.Magician, i] =
                    (2 * i) + 9; // approx Numbers n such that n^2 is of form x^2+40y^2 with positive x,y.
                _distDef[(int) CharacterClassType.Magician, i] = 20 + i; // approx

                // ARCHER
                _criticalHitRate[(int) CharacterClassType.Archer, i] = 0; // sure
                _criticalHit[(int) CharacterClassType.Archer, i] = 0; // sure
                _criticalDistRate[(int) CharacterClassType.Archer, i] = 0; // sure
                _criticalDist[(int) CharacterClassType.Archer, i] = 0; // sure

                _minHit[(int) CharacterClassType.Archer, i] = 9 + (i * 3); // approx
                _maxHit[(int) CharacterClassType.Archer, i] = 9 + (i * 3); // approx
                var add = i % 2 == 0 ? 2 : 4;
                _hitRate[(int) CharacterClassType.Archer, 1] = 41;
                _hitRate[(int) CharacterClassType.Archer, i] += add; // approx
                _minDist[(int) CharacterClassType.Archer, i] = 2 * i; // approx
                _maxDist[(int) CharacterClassType.Archer, i] = 2 * i; // approx

                _distRate[(int) CharacterClassType.Archer, i] = 20 + (2 * i); // approx
                _hitDef[(int) CharacterClassType.Archer, i] = i; // approx
                _magicalDef[(int) CharacterClassType.Archer, i] = i + 2; // approx
                _hitDodge[(int) CharacterClassType.Archer, i] = 41 + i; // approx
                _distDodge[(int) CharacterClassType.Archer, i] = i + 2; // approx
                _distDef[(int) CharacterClassType.Archer, i] = i; // approx
            }
        }

        private void LoadXpData()
        {
            // Load XpData
            XpData = new double[256];
            var v = new double[256];
            double var = 1;
            v[0] = 540;
            v[1] = 960;
            XpData[0] = 300;
            for (var i = 2; i < v.Length; i++)
            {
                v[i] = v[i - 1] + 420 + (120 * (i - 1));
            }

            for (var i = 1; i < XpData.Length; i++)
            {
                if (i < 79)
                {
                    if (i == 14)
                    {
                        var = 6 / 3d;
                    }
                    else if (i == 39)
                    {
                        var = 19 / 3d;
                    }
                    else if (i == 59)
                    {
                        var = 70 / 3d;
                    }

                    XpData[i] = Convert.ToInt64(XpData[i - 1] + (var * v[i - 1]));
                }

                if (i < 79)
                {
                    continue;
                }

                switch (i)
                {
                    case 79:
                        var = 5000;
                        break;
                    case 82:
                        var = 9000;
                        break;
                    case 84:
                        var = 13000;
                        break;
                }

                XpData[i] = Convert.ToInt64(XpData[i - 1] + (var * (i + 2) * (i + 2)));
            }
        }

        #endregion
    }
}