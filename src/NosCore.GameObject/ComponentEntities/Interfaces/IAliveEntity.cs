//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.ShopService;
using System.Collections.Concurrent;
using System.Threading;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface IAliveEntity : IVisualEntity
    {
        bool IsSitting { get; set; }

        byte Speed { get; }

        byte Size { get; set; }

        int Mp { get; set; }

        int Hp { get; set; }

        short Morph { get; }

        byte MorphUpgrade { get; }

        short MorphDesign { get; }

        byte MorphBonus { get; }

        bool NoAttack { get; }

        bool NoMove { get; }

        bool IsAlive { get; }

        short MapX { get; }

        short MapY { get; }

        int MaxHp { get; }

        int MaxMp { get; }

        byte Level { get; set; }

        byte HeroLevel { get; }

        short Race { get; }

        Shop? Shop { get; }

        SemaphoreSlim HitSemaphore { get; }

        ConcurrentDictionary<IAliveEntity, int> HitList { get; }
    }
}
