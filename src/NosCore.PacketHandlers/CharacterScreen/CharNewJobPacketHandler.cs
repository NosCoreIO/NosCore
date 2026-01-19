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
            //TODO add a flag on Account
            if (await characterDao.FirstOrDefaultAsync(s =>
                (s.Level >= 80) && (s.AccountId == clientSession.Account.AccountId) && (s.ServerId == configuration.Value.ServerId) &&
                (s.State == CharacterState.Active)) == null)
            {
                //Needs at least a level 80 to Create a martial artist
                //TODO log
                return;
            }

            if (await characterDao.FirstOrDefaultAsync(s =>
                (s.AccountId == clientSession.Account.AccountId) &&
                (s.Class == CharacterClassType.MartialArtist) && (s.State == CharacterState.Active)) != null)
            {
                //If already a martial artist, can't Create another
                //TODO log
                return;
            }
            //todo add cooldown for recreate 30days

            await clientSession.HandlePacketsAsync(new[] { packet.Adapt<CharNewPacket>() });
        }
    }
}
