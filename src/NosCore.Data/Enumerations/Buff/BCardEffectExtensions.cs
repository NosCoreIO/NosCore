//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;

namespace NosCore.Data.Enumerations.Buff
{
    public static class BCardEffectExtensions
    {
        public static BCardEffect Effect(this BCardDto card) => Effect(card.Type, card.SubType);

        public static BCardEffect Effect(byte type, byte subType) => (BCardEffect)(type * 100 + subType);

        public static byte Type(this BCardEffect effect) => (byte)((short)effect / 100);

        public static byte SubType(this BCardEffect effect) => (byte)((short)effect % 100);
    }
}
