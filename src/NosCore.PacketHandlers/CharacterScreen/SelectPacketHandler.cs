//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class SelectPacketHandler(IDao<CharacterDto, long> characterDao, ILogger<SelectPacketHandler> logger,
            IOptions<WorldConfiguration> configuration, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            IPubSubHub pubSubHub, ICharacterLoadService characterLoadService,
            ICharacterInitializationService characterInitializationService)
        : PacketHandler<SelectPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SelectPacket packet, ClientSession clientSession)
        {
            var characterDto = await
                characterDao.FirstOrDefaultAsync(s =>
                    (s.AccountId == clientSession.Account.AccountId) && (s.Slot == packet.Slot)
                    && (s.State == CharacterState.Active) && s.ServerId == configuration.Value.ServerId);
            if (characterDto == null)
            {
                logger.LogError(logLanguage[LogLanguageKey.CHARACTER_SLOT_EMPTY], new
                {
                    clientSession.Account.AccountId,
                    packet.Slot
                });
                return;
            }

            await pubSubHub.SubscribeAsync(new Subscriber
            {
                Id = clientSession.SessionId,
                Name = clientSession.Account.Name,
                Language = clientSession.Account.Language,
                ConnectedCharacter = new Data.WebApi.Character
                {
                    Name = characterDto.Name ?? string.Empty,
                    Id = characterDto.CharacterId,
                    FriendRequestBlocked = characterDto.FriendRequestBlocked,
                    WhisperBlocked = characterDto.WhisperBlocked
                }
            });

            var character = await characterLoadService.LoadAsync(clientSession, characterDto);

#pragma warning disable CS0618
            await clientSession.SendPacketsAsync(character.GenerateInv(logger, logLanguage));
#pragma warning restore CS0618
            await clientSession.SendPacketAsync(character.GenerateMlobjlst());

            await characterInitializationService.InitializeAsync(character);

            await clientSession.SendPacketAsync(new OkPacket());
        }
    }
}
