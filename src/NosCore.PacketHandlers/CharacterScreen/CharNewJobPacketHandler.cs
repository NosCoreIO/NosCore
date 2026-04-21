//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CharNewJobPacketHandler(IDao<CharacterDto, long> characterDao,
            IOptions<WorldConfiguration> configuration)
        : PacketHandler<CharNewJobPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CharNewJobPacket packet, ClientSession clientSession)
        {
            if (clientSession.HasSelectedCharacter)
            {
                return;
            }

            var targetClass = MapJobClass(packet.JobClass);
            if (targetClass == null)
            {
                return;
            }

            if (targetClass == CharacterClassType.MartialArtist)
            {
                var alreadyMartialArtist = await characterDao.FirstOrDefaultAsync(s =>
                    (s.AccountId == clientSession.Account.AccountId) &&
                    (s.Class == CharacterClassType.MartialArtist) && (s.State == CharacterState.Active));
                if (alreadyMartialArtist != null)
                {
                    return;
                }

                if (!configuration.Value.AllClassAvailableOnCreate)
                {
                    var hasLevel80 = await characterDao.FirstOrDefaultAsync(s =>
                        (s.Level >= 80) && (s.AccountId == clientSession.Account.AccountId) && (s.ServerId == configuration.Value.ServerId) &&
                        (s.State == CharacterState.Active));
                    if (hasLevel80 == null)
                    {
                        return;
                    }
                    //todo add cooldown for recreate 30days
                }
            }

            var forwarded = packet.Adapt<CharNewPacket>();
            forwarded.TargetClass = (byte)targetClass.Value;
            await clientSession.HandlePacketsAsync(new[] { forwarded });
        }

        private static CharacterClassType? MapJobClass(byte? jobClass) => jobClass switch
        {
            1 => CharacterClassType.MartialArtist,
            2 => CharacterClassType.Swordsman,
            3 => CharacterClassType.Archer,
            4 => CharacterClassType.Mage,
            _ => null
        };
    }
}
