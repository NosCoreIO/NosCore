//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.PenaltyService
{
    public static class MutedSpeakerExtensions
    {
        // True when the account is muted, after telling the speaker how many minutes are left.
        public static async Task<bool> RefuseMutedSpeakerAsync(this IPenaltyService penaltyService, ClientSession session)
        {
            if (await penaltyService.GetMuteLeftAsync(session.Account.AccountId) is not { } left)
            {
                return false;
            }

            await session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Red,
                Game18NConstString.RemainingPunishment, 4, (long)Math.Ceiling(left.TotalMinutes)));
            return true;
        }
    }
}
