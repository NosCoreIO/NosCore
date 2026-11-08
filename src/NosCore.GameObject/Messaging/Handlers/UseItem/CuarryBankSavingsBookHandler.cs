//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Algorithm.BankService;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class CuarryBankSavingsBookHandler(IBankService bankService, INpcDialogService npcDialogService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            if (evt.InventoryItem.ItemInstance.Item.Effect != ItemEffectType.CuarryBankSavingsBook)
            {
                return;
            }

            var session = evt.ClientSession;
            npcDialogService.OpenRemotely(session.Character, NpcDialogWindow.Bank);
            await session.SendPacketAsync(session.Character.GenerateGb(BankActionType.OpenFromSavingsBook, bankService));
            await session.SendPacketAsync(session.Character.GenerateBankBalance());
        }
    }
}
