//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using JetBrains.Annotations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class WeddingGazeboTeleporterHandler(IMapChangeService mapChangeService, List<TeleporterDto> teleporters)
        : NpcTeleportHandler(mapChangeService, teleporters)
    {
        public override NrunRunnerType Runner => NrunRunnerType.GoToWeddingGazebo;

        protected override long Cost(short index) => 500;
    }
}
