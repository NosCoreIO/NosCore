//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Shared.Enumerations;
using System;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface IVisualEntity
    {
        VisualType VisualType { get; }

        short VNum { get; }

        long VisualId { get; }

        byte Direction { get; set; }

        Guid MapInstanceId { get; }

        MapInstance MapInstance { get; }

        short PositionX { get; set; }

        short PositionY { get; set; }
    }
}
