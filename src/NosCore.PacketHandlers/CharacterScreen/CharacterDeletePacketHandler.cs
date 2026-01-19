//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.CommandPackets;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Authentication;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CharacterDeletePacketHandler(IDao<CharacterDto, long> characterDao, IDao<AccountDto, long> accountDao,
            IHasher hasher, IOptions<WorldConfiguration> configuration)
        : PacketHandler<CharacterDeletePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CharacterDeletePacket packet, ClientSession clientSession)
        {
            var account = await accountDao
                .FirstOrDefaultAsync(s => s.AccountId.Equals(clientSession.Account.AccountId));
            if (account == null)
            {
                return;
            }

            if ((account.Password!.ToLower() == hasher.Hash(packet.Password!)) || (account.Name == packet.Password))
            {
                var character = await characterDao.FirstOrDefaultAsync(s =>
                    (s.AccountId == account.AccountId) && (s.Slot == packet.Slot) && (s.ServerId == configuration.Value.ServerId)
                    && (s.State == CharacterState.Active));
                if (character == null)
                {
                    return;
                }

                character.State = CharacterState.Inactive;
                character = await characterDao.TryInsertOrUpdateAsync(character);

                await clientSession.HandlePacketsAsync(new[]
                {
                    new EntryPointPacket
                    {
                        Header = "EntryPoint",
                        Name = account.Name,
                        Password = account.Password
                    }
                });
            }
            else
            {
                await clientSession.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.IncorrectPassword
                });
            }
        }
    }
}
