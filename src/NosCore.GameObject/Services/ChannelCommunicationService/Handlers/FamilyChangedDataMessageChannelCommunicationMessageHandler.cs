//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.FamilyExperienceService;
using NosCore.Core.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class FamilyChangedDataMessageChannelCommunicationMessageHandler(ISessionRegistry sessionRegistry,
            IFamilyService familyService, IGameLanguageLocalizer gameLanguageLocalizer,
            IFamilyExperienceService familyExperienceService)
        : ChannelCommunicationMessageHandler<FamilyChangedData>
    {
        public override async Task Handle(FamilyChangedData data)
        {
            var sessions = sessionRegistry.GetSessions(s => s.HasSelectedCharacter
                && (s.Character.Family?.FamilyId == data.FamilyId || data.CharacterIds.Contains(s.Character.CharacterId)))
                .ToList();
            foreach (var session in sessions)
            {
                var character = session.Character;
                character.Family = await familyService.GetFamilyAsync(character.CharacterId);
                var info = character.GenerateGInfo(familyExperienceService);
                if (info != null)
                {
                    await session.SendPacketAsync(info);
                }

                if (data.JoinedCharacterName != null && character.Family != null)
                {
                    await session.SendPacketAsync(new Msgi2Packet
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.JoinedFamily,
                        ArgumentType = 1,
                        Game18NArguments = { data.JoinedCharacterName }
                    });
                }

                if (!data.CharacterIds.Contains(character.CharacterId))
                {
                    continue;
                }

                var watchers = sessionRegistry.GetSessions(s => s.HasPlayerEntity && s.Character.MapInstanceId == character.MapInstanceId);
                await Task.WhenAll(watchers.Select(watcher =>
                    watcher.SendPacketAsync(character.GenerateGidx(gameLanguageLocalizer, watcher.Character.AccountLanguage))));
            }
        }
    }
}
