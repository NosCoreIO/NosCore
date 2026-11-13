//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class FamilyChangedDataMessageChannelCommunicationMessageHandler(ISessionRegistry sessionRegistry,
            IFamilyService familyService, IGameLanguageLocalizer gameLanguageLocalizer,
            IFamilyManagementService familyManagementService)
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
                await familyManagementService.SendMemberListAsync(session);

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

                if (data.ReachedFamilyLevel is { } familyLevel && character.Family?.FamilyId == data.FamilyId)
                {
                    await session.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.FamilyLevelIncreased,
                        ArgumentType = 4,
                        Game18NArguments = { (long)familyLevel }
                    });
                }

                if (character.Family?.FamilyId == data.FamilyId && character.Family is { } family)
                {
                    await SendUnlockedAchievementsAsync(session, family, data.CompletedQuestIds);
                }

                if (!data.CharacterIds.Contains(character.CharacterId))
                {
                    continue;
                }

                // The history change that came with the join reached this session before it had the family.
                if (character.Family != null)
                {
                    await session.SendPacketAsync(new FhisStcPacket());
                }

                var watchers = sessionRegistry.GetSessions(s => s.HasPlayerEntity && s.Character.MapInstanceId == character.MapInstanceId);
                await Task.WhenAll(watchers.Select(watcher =>
                    watcher.SendPacketAsync(character.GenerateGidx(gameLanguageLocalizer, watcher.Character.AccountLanguage))));
            }
        }

        private async Task SendUnlockedAchievementsAsync(ClientSession session, Family family, List<short> completedQuestIds)
        {
            foreach (var quest in family.Quests)
            {
                if (quest.Quest.Kind != FamilyQuestKind.Achievement || !completedQuestIds.Contains(quest.Quest.QuestId))
                {
                    continue;
                }

                await session.SendPacketAsync(new Msgi2Packet
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.FamilyUnlockAchievement,
                    ArgumentType = 5,
                    Game18NArguments = { family.Name, (long)quest.Quest.NameItemVNum }
                });
            }
        }
    }
}