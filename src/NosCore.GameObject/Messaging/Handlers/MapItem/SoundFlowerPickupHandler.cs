//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.QuestService;
using NosCore.Networking;

namespace NosCore.GameObject.Messaging.Handlers.MapItem
{
    [UsedImplicitly]
    public sealed class SoundFlowerPickupHandler(ISoundFlowerService soundFlowerService)
    {
        [UsedImplicitly]
        public async Task Handle(MapItemPickedUpEvent evt)
        {
            if (evt.ItemInstance?.Item.Effect != ItemEffectType.SoundFlower)
            {
                return;
            }

            // The quest goes first so that a flower whose quest is refused stays on the ground.
            var mapInstance = evt.ClientSession.Character.MapInstance;
            if (!await soundFlowerService.StartFlowerQuestAsync(evt.ClientSession.Character)
                || !mapInstance.TryRemoveMapItem(evt.VisualId))
            {
                return;
            }

            await mapInstance.SendPacketAsync(evt.GenerateGet());
        }
    }
}
