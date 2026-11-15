//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Algorithm.FamilyExperienceService;
using NosCore.GameObject.Services.CharacterRankingService;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ChannelService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.SkillService;
using NosCore.GameObject.Services.WarehouseService;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using NodaTime;
using NosCore.Data.StaticEntities;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.PacketHandlers.Game
{
    public class GameStartPacketHandler(IOptions<WorldConfiguration> worldConfiguration,
            IFriendHub friendHttpClient,
            IChannelHub channelHttpClient,
            IPubSubHub pubSubHub, IBlacklistHub blacklistHttpClient,
            ISerializer packetSerializer, IMailHub mailHttpClient, IQuestService questProvider,
            IMapChangeService mapChangeService, ISkillService skillService,
            IFamilyExperienceService familyExperienceService, ILogger<GameStartPacketHandler> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            IBuffService buffService, IVitalityService vitalityService,
            ICharacterRankingService characterRankingService, IPersonalWarehouseService warehouseService,
            IRespawnService respawnService, IMinilandRegistry minilandRegistry, List<QuestDto> quests, IClock clock,
            IChannelService channelService, IPartnerSkillCatalog partnerSkillCatalog)
        : PacketHandler<GameStartPacket>, IWorldPacketHandler
    {
        /// <summary>
        /// Initializes world entry and sends the character's current state, including the persisted faction.
        /// </summary>
        public override async Task ExecuteAsync(GameStartPacket packet, ClientSession session)
        {
            if (session.GameStarted || !session.HasSelectedCharacter)
            {
                // character should have been selected in SelectCharacter
                return;
            }

            session.GameStarted = true;

            var character = session.Character;
            if (character.Hp > 0 && character.MapInstance is { } map
                && await channelService.TryMoveToHostAsync(session, map, character.MapX, character.MapY))
            {
                return;
            }

            if (worldConfiguration.Value.WorldInformation)
            {
                await session.SendPacketAsync(session.Character.GenerateSay("-------------------[NosCore]---------------",
                    SayColorType.Yellow));
                await session.SendPacketAsync(session.Character.GenerateSay("Github : https://github.com/NosCoreIO/NosCore/",
                    SayColorType.Red));
                await session.SendPacketAsync(session.Character.GenerateSay("-----------------------------------------------",
                    SayColorType.Yellow));
            }

            await skillService.LoadSkill(session.Character);
            vitalityService.Refresh(session.Character);
            await session.SendPacketAsync(session.Character.GenerateTit());
            await session.SendPacketAsync(session.Character.GenerateSpPoint(worldConfiguration));
            await session.SendPacketAsync(session.Character.GenerateRsfi());
            await session.SendPacketAsync(session.Character.GenerateQuestPacket());
            await session.SendPacketsAsync(session.Character.GenerateCompletedQuests(quests, clock.GetCurrentInstant()));

            if (session.Character.Hp <= 0)
            {
                await ComeBackToLifeAtTheSavePointAsync(session);
            }
            else
            {
                await mapChangeService.ChangeMapAsync(session);
            }

            if (session.Character.CurrentScriptId == null)
            {
                try
                {
                    await questProvider.RunScriptAsync(session.Character);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, logLanguage[LogLanguageKey.FIRST_QUEST_SCRIPT_FAILED],
                        session.Character.CharacterId);
                }
            }
            else if (session.Character.Script is { } currentStep)
            {
                await session.SendPacketAsync(new ScriptPacket
                {
                    ScriptId = currentStep.ScriptId,
                    ScriptStepId = currentStep.ScriptStepId,
                });
            }

            await session.SendPacketAsync(session.Character.GenerateFd());
            await session.SendPacketAsync(session.Character.GenerateStat());
            if (session.Character.BazaarMedal != null)
            {
                await session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Green, Game18NConstString.NosMerchantActive));
            }

            await warehouseService.SendCarriedStoragesAsync(session);

            session.Character.LoadExpensions();
            await session.SendPacketAsync(session.Character.GenerateExts(worldConfiguration));
            if (minilandRegistry.GetByCharacterId(session.Character.CharacterId) is { } miniland)
            {
                await session.SendPacketAsync(miniland.GenerateMlinfo());
            }

            await session.SendPacketAsync(new TwkPacket(session.Account.Name, session.Character.Name)
            {
                VisualId = session.Character.VisualId,
                VisualType = VisualType.Player,
                AccountName = session.Account.Name,
                ClientLanguage = session.Account.Language,
                ServerLanguage = worldConfiguration.Value.Language
            });

            await session.SendPacketsAsync(session.Character.Quests.Values.Where(o => o.CompletedOn == null).Select(qst => qst.Quest.GenerateTargetPacket()));
            await session.SendPacketAsync(session.Character.GenerateFaction());
            // p_clear wipes one panel holding both the party and the mate list, so both bursts
            // have to follow it rather than straddle it.
            await session.SendPacketsAsync(session.Character.GenerateMateRoster(partnerSkillCatalog));
            await session.SendPacketAsync(new ScPStcPacket { MaxMateCountTenths = 0 });

            await session.SendPacketAsync(session.Character.GeneratePinit());
            await session.SendPacketsAsync(session.Character.Group.GeneratePst());
            await session.SendPacketsAsync(session.Character.Mates.Values
                .Where(s => s.IsTeamMember).Select(s => s.GeneratePst()));

            var familyInfo = session.Character.GenerateGInfo(familyExperienceService);
            if (familyInfo != null)
            {
                await session.SendPacketAsync(familyInfo);
                await session.SendPacketAsync(session.Character.GenerateFmi());
                await session.SendPacketAsync(new FhisStcPacket());
            }

            await session.SendPacketAsync(session.Character.GenerateGold());
            await session.SendPacketAsync(session.Character.GenerateCond());

            await session.SendPacketAsync(session.Character.GenerateSki(useSpecialist: false));
            await session.SendPacketsAsync(session.Character.GenerateQuicklist());


            await session.Character.SendFinfoAsync(friendHttpClient, pubSubHub, packetSerializer, true);

            await session.SendPacketAsync(await session.Character.GenerateFinitAsync(friendHttpClient, channelHttpClient, pubSubHub));
            await session.SendPacketAsync(await session.Character.GenerateBlinitAsync(blacklistHttpClient));
            var rankings = characterRankingService.GetRankings();
            await session.SendPacketAsync(rankings.GeneratePopularityList());
            await session.SendPacketAsync(rankings.GenerateReputationList());
            await session.SendPacketAsync(rankings.GenerateCombatPointsList());





            var mails = await mailHttpClient.GetMails(-1, session.Character.CharacterId, false);
            await session.GenerateMailAsync(mails);

            await session.SendPacketAsync(session.Character.GenerateTitle());
            int giftcount = mails.Select(s => s.MailDto).Count(mail => !mail.IsSenderCopy && mail.ReceiverId == session.Character.CharacterId && mail.ItemInstanceId != null && !mail.IsOpened);
            int mailcount = mails.Select(s => s.MailDto).Count(mail => !mail.IsSenderCopy && mail.ReceiverId == session.Character.CharacterId && mail.ItemInstanceId == null && !mail.IsOpened);

            if (giftcount > 0)
            {
                await session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Green, Game18NConstString.NewParcelArrived,
                    4, giftcount));
            }

            if (mailcount > 0)
            {
                await session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Green, Game18NConstString.NewNoteArrived,
                    4, mailcount));
            }

            await buffService.ShowStaticAsync(session.Character);


        }

        // A character that left the world dead is back on its feet at its save point, barely alive.
        private async Task ComeBackToLifeAtTheSavePointAsync(ClientSession session)
        {
            var character = session.Character;
            var (mapId, x, y) = respawnService.GetRespawnLocation(character,
                respawnService.ResolveRespawnMapTypeId(character.MapId));
            character.Hp = 1;
            character.Mp = 1;
            character.IsAlive = true;
            await mapChangeService.ChangeMapAsync(session, mapId, x, y);
        }
    }
}
