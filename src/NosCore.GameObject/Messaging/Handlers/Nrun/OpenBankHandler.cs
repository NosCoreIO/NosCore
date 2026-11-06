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

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class OpenBankHandler(IBankService bankService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.OpenBankFacilities;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target == null)
            {
                return;
            }

            var character = session.Character;
            if (character.InShop || character.InExchange)
            {
                await session.SendPacketAsync(new SMemoiPacket
                {
                    Type = SMemoType.BankError,
                    Message = character.InShop ? Game18NConstString.NoBankInShop : Game18NConstString.NoBankExchange
                });
                return;
            }

            await session.SendPacketAsync(character.GenerateGb(BankActionType.OpenBank, bankService));
        }
    }
}
