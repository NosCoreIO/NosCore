//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;

namespace NosCore.GameObject.Messaging.Handlers.Quest
{
    [UsedImplicitly]
    public sealed class QuestDignityHandler
    {
        [UsedImplicitly]
        public Task Handle(QuestCompletedEvent evt) => evt.Character.RestoreDignityAsync();
    }
}
