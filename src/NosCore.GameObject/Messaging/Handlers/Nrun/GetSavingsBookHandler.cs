//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class GetSavingsBookHandler(IItemGenerationService itemProvider) : INrunEventHandler
    {
        public const short SavingsBookVNum = 5836;

        public NrunRunnerType Runner => NrunRunnerType.GetSavingsBook;

        public bool RequiresNpc => true;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            var character = session.Character;
            if (target is not NpcComponentBundle || character.InventoryService.Values.Any(i => i.ItemInstance.ItemVNum == SavingsBookVNum))
            {
                return;
            }

            var book = itemProvider.Create(SavingsBookVNum, 1);
            var added = character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(book, character.CharacterId))?.FirstOrDefault();
            if (added == null)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.NotEnoughSpace
                });
                return;
            }

            await session.SendPacketAsync(added.GeneratePocketChange((PocketType)added.Type, added.Slot));
            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = character.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.ReceivedThisItem,
                ArgumentType = 2,
                Game18NArguments = { SavingsBookVNum.ToString(), 1 }
            });
        }
    }
}
