//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.GroupService;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface INamedEntity : IAliveEntity
    {
        Group? Group { get; set; }

        string? Name { get; }

        long LevelXp { get; set; }
    }
}
