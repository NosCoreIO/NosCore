//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.BankService;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Bank;
using NosCore.Packets.ServerPackets.Shop;

namespace NosCore.GameObject.Services.BankService;

public static class BankPackets
{
    public static GbPacket GenerateGb(this PlayerComponentBundle player, BankActionType type, IBankService bankService)
    {
        return new GbPacket
        {
            Type = type,
            GoldBank = player.BankGold / 1000,
            Gold = (int)player.Gold,
            BankRank = player.GetBankRank(bankService),
            BankTax = (int)player.GetBankWithdrawalFee(bankService)
        };
    }

    public static SMemoi2Packet GenerateBankBalance(this PlayerComponentBundle player) => new()
    {
        Type = SMemoType.BankNormal,
        Message = Game18NConstString.BalanceBank,
        ArgumentType = 3,
        Game18NArguments = { player.BankGold / 1000, player.Gold }
    };
}
