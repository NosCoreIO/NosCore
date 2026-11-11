//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Algorithm.BankService;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.NpcDialogService;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class OpenBankHandler(IBankService bankService, INpcDialogService npcDialogService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.OpenBankFacilities;

        public bool RequiresNpc => true;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not NpcComponentBundle npc)
            {
                return;
            }

            var character = session.Character;
            npcDialogService.Open(character, npc, NpcDialogWindow.Bank);
            await session.SendPacketAsync(character.GenerateGb(BankActionType.OpenBank, bankService));
        }
    }
}
