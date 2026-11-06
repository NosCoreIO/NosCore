//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NosCore.Algorithm.BankService;
using NosCore.Core.Configuration;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Bank;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.PacketHandlers.Bank
{
    public class GboxPacketHandler(IBankService bankService, IOptions<WorldConfiguration> worldConfiguration)
        : PacketHandler<GboxPacket>, IWorldPacketHandler
    {
        private const long GoldPerUnit = 1000;

        public override async Task ExecuteAsync(GboxPacket packet, ClientSession clientSession)
        {
            var character = clientSession.Character;
            if (packet.Amount <= 0 || packet.Type is not (BankActionType.DepositGold or BankActionType.WithdrawGold))
            {
                return;
            }

            if (character.InShop || character.InExchange)
            {
                await SendErrorAsync(clientSession,
                    character.InShop ? Game18NConstString.NoBankInShop : Game18NConstString.NoBankExchange);
                return;
            }

            if (packet.Amount > worldConfiguration.Value.MaxBankGoldAmount / GoldPerUnit)
            {
                return;
            }

            var gold = packet.Amount * GoldPerUnit;
            if (packet.Type == BankActionType.DepositGold)
            {
                await DepositAsync(packet, clientSession, character, gold);
            }
            else
            {
                await WithdrawAsync(packet, clientSession, character, gold);
            }
        }

        private async Task DepositAsync(GboxPacket packet, ClientSession clientSession, PlayerComponentBundle character, long gold)
        {
            if (character.Gold < gold)
            {
                await SendErrorAsync(clientSession, Game18NConstString.NotEnoughGold3);
                return;
            }

            if (character.BankGold + gold > worldConfiguration.Value.MaxBankGoldAmount)
            {
                await SendErrorAsync(clientSession, Game18NConstString.CanNotDepositAnymore);
                return;
            }

            if (!packet.Mode)
            {
                await clientSession.SendPacketAsync(new Qnai2Packet
                {
                    YesPacket = new GboxPacket { Type = BankActionType.DepositGold, Amount = packet.Amount, Mode = true },
                    Question = Game18NConstString.AskDeposit,
                    ArgumentType = 1,
                    Game18NArguments = { packet.Amount }
                });
                return;
            }

            character.Gold -= gold;
            character.BankGold += gold;
            await SendResultAsync(clientSession, character, BankActionType.DepositGold, new SMemoi2Packet
            {
                Type = SMemoType.BankSuccess,
                Message = Game18NConstString.DepositBank,
                ArgumentType = 2,
                Game18NArguments = { packet.Amount }
            });
        }

        private async Task WithdrawAsync(GboxPacket packet, ClientSession clientSession, PlayerComponentBundle character, long gold)
        {
            var fee = character.GetBankWithdrawalFee(bankService);
            if (character.BankGold < gold)
            {
                await SendErrorAsync(clientSession, Game18NConstString.NotEnoughFounds);
                return;
            }

            if (character.Gold < fee)
            {
                await SendErrorAsync(clientSession, Game18NConstString.YouDontHaveAnyGoldToPay);
                return;
            }

            if (character.Gold - fee + gold > worldConfiguration.Value.MaxGoldAmount)
            {
                await SendErrorAsync(clientSession, Game18NConstString.MaxGoldReached);
                return;
            }

            if (!packet.Mode)
            {
                await clientSession.SendPacketAsync(new Qnai2Packet
                {
                    YesPacket = new GboxPacket { Type = BankActionType.WithdrawGold, Amount = packet.Amount, Mode = true },
                    Question = Game18NConstString.AskWithdraw,
                    ArgumentType = 2,
                    Game18NArguments = { packet.Amount, fee }
                });
                return;
            }

            character.BankGold -= gold;
            character.Gold += gold - fee;
            await SendResultAsync(clientSession, character, BankActionType.WithdrawGold, new SMemoi2Packet
            {
                Type = SMemoType.BankSuccess,
                Message = Game18NConstString.WithdrawBank,
                ArgumentType = 3,
                Game18NArguments = { packet.Amount, fee }
            });
        }

        private async Task SendResultAsync(ClientSession clientSession, PlayerComponentBundle character,
            BankActionType type, SMemoi2Packet result)
        {
            await clientSession.SendPacketAsync(character.GenerateGold());
            await clientSession.SendPacketAsync(character.GenerateGb(type, bankService));
            await clientSession.SendPacketAsync(result);
            await clientSession.SendPacketAsync(character.GenerateBankBalance());
        }

        private static Task SendErrorAsync(ClientSession clientSession, Game18NConstString message)
        {
            return clientSession.SendPacketAsync(new SMemoiPacket
            {
                Type = SMemoType.BankError,
                Message = message
            });
        }
    }
}
