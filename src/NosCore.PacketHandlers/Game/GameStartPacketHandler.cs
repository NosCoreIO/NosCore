//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Systems;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.SkillService;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.MailService;

namespace NosCore.PacketHandlers.Game
{
    public class GameStartPacketHandler(IOptions<WorldConfiguration> worldConfiguration,
            IFriendHub friendHttpClient,
            IChannelHub channelHttpClient,
            IPubSubHub pubSubHub, IBlacklistHub blacklistHttpClient,
            ISerializer packetSerializer, IMailHub mailHttpClient, IQuestService questProvider,
            IMapChangeService mapChangeService, ISkillService skillService, ICondSystem condSystem,
            IEntityPacketSystem entityPacketSystem, ICharacterPacketSystem characterPacketSystem,
            IMailService mailService)
        : PacketHandler<GameStartPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(GameStartPacket packet, ClientSession session)
        {
            if (session.GameStarted || !session.HasSelectedCharacter)
            {
                // character should have been selected in SelectCharacter
                return;
            }

            session.GameStarted = true;

            if (session.Player.CharacterData.CurrentScriptId == null)
            {
                _ = questProvider.RunScriptAsync(session.Player);
            }

            if (worldConfiguration.Value.WorldInformation)
            {
                await session.SendPacketAsync(entityPacketSystem.GenerateSay(session.Player, "-------------------[NosCore]---------------",
                    SayColorType.Yellow)).ConfigureAwait(false);
                await session.SendPacketAsync(entityPacketSystem.GenerateSay(session.Player, "Github : https://github.com/NosCoreIO/NosCore/",
                    SayColorType.Red)).ConfigureAwait(false);
                await session.SendPacketAsync(entityPacketSystem.GenerateSay(session.Player, "-----------------------------------------------",
                    SayColorType.Yellow)).ConfigureAwait(false);
            }


            await skillService.LoadSkill(session.Player);
            await session.SendPacketAsync(characterPacketSystem.GenerateTit(session.Player)).ConfigureAwait(false);
            await session.SendPacketAsync(session.Player.GenerateSpPoint(worldConfiguration.Value.MaxSpPoints, worldConfiguration.Value.MaxAdditionalSpPoints)).ConfigureAwait(false);
            await session.SendPacketAsync(characterPacketSystem.GenerateRsfi(session.Player)).ConfigureAwait(false);
            await session.SendPacketAsync(characterPacketSystem.GenerateQuestPacket(session.Player)).ConfigureAwait(false);

            if (session.Player.CharacterData.Hp <= 0)
            {
                //                ServerManager.Instance.ReviveFirstPosition(Session.Character.CharacterId);
            }
            else
            {
                await mapChangeService.ChangeMapAsync(session).ConfigureAwait(false);
            }

            //            Session.SendPacket(Session.Character.GenerateSki());
            //            Session.SendPacket($"fd {Session.Character.Reput} 0 {(int)Session.Character.Dignity} {Math.Abs(Session.Character.GetDignityIco())}");
            await session.SendPacketAsync(characterPacketSystem.GenerateFd(session.Player)).ConfigureAwait(false);
            await session.SendPacketAsync(session.Player.GenerateStat()).ConfigureAwait(false);
            //            Session.SendPacket("rage 0 250000");
            //            Session.SendPacket("rank_cool 0 0 18000");
            //            SpecialistInstance specialistInstance = Session.Character.Inventory.LoadBySlotAndType<SpecialistInstance>(8, InventoryType.Wear);
            var medal = session.Player.StaticBonusList.FirstOrDefault(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);
            if (medal != null)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Player.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.NosMerchantActive,
                }).ConfigureAwait(false);
            }

            //            if (Session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.PetBasket))
            //            {
            //                Session.SendPacket("ib 1278 1");
            //            }

            //            if (Session.Character.MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.CleftOfDarkness))
            //            {
            //                Session.SendPacket("bc 0 0 0");
            //            }
            //            if (specialistInstance != null)
            //            {
            //                Session.SendPacket(Session.Character.GenerateSpPoint());
            //            }
            //            Session.SendPacket("scr 0 0 0 0 0 0");
            //            for (int i = 0; i < 10; i++)
            //            {
            //                Session.SendPacket($"bn {i} {Language.Instance.GetMessageFromKey($"BN{i}")}");
            //            }
            session.Player.LoadExpensions();
            await session.SendPacketAsync(characterPacketSystem.GenerateExts(session.Player, worldConfiguration)).ConfigureAwait(false);
            //            Session.SendPacket(Session.Character.GenerateMlinfo());
            await session.SendPacketAsync(new PclearPacket()).ConfigureAwait(false);

            //            Session.SendPacket(Session.Character.GeneratePinit());
            //            Session.SendPackets(Session.Character.GeneratePst());

            //            Session.SendPacket("zzim");
            await session.SendPacketAsync(new TwkPacket(session.Account.Name, session.Player.CharacterData.Name)
            {
                VisualId = session.Player.VisualId,
                VisualType = VisualType.Player,
                AccountName = session.Account.Name,
                ClientLanguage = session.Account.Language,
                ServerLanguage = worldConfiguration.Value.Language
            });
            //            Session.SendPacket($"twk 2 {Session.Character.CharacterId} {Session.Account.Name} {Session.Character.Name} shtmxpdlfeoqkr");

            await session.SendPacketsAsync(session.Player.Quests.Values.Where(o => o.CompletedOn == null).Select(qst => qst.Quest.GenerateTargetPacket())).ConfigureAwait(false);
            //            // sqst bf
            //            Session.SendPacket("act6");
            //            Session.SendPacket(Session.Character.GenerateFaction());
            //            // MATES
            //            Session.SendPackets(Session.Character.GenerateScP());
            //            Session.SendPackets(Session.Character.GenerateScN());
            //            Session.Character.GenerateStartupInventory();

            await session.SendPacketAsync(characterPacketSystem.GenerateGold(session.Player)).ConfigureAwait(false);
            await session.SendPacketAsync(condSystem.GenerateCondPacket(session.Player)).ConfigureAwait(false);

            await session.SendPacketAsync(characterPacketSystem.GenerateSki(session.Player));
            await session.SendPacketsAsync(characterPacketSystem.GenerateQuicklist(session.Player)).ConfigureAwait(false);

            //            string clinit = ServerManager.Instance.TopComplimented.Aggregate("clinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Compliment}|{character.Name}");
            //            string flinit = ServerManager.Instance.TopReputation.Aggregate("flinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Reput}|{character.Name}");
            //            string kdlinit = ServerManager.Instance.TopPoints.Aggregate("kdlinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Act4Points}|{character.Name}");

            //            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateGidx());

            await characterPacketSystem.SendFinfoAsync(session.Player, friendHttpClient, pubSubHub, packetSerializer, true).ConfigureAwait(false);

            await session.SendPacketAsync(await characterPacketSystem.GenerateFinitAsync(session.Player, friendHttpClient, channelHttpClient, pubSubHub).ConfigureAwait(false)).ConfigureAwait(false);
            await session.SendPacketAsync(await characterPacketSystem.GenerateBlinitAsync(session.Player, blacklistHttpClient).ConfigureAwait(false)).ConfigureAwait(false);
            //            Session.SendPacket(clinit);
            //            Session.SendPacket(flinit);
            //            Session.SendPacket(kdlinit);

            //            Session.Character.LastPVPRevive = SystemTime.Now;

            //            long? familyId = _familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == Session.Character.CharacterId)?.FamilyId;
            //            if (familyId != null)
            //            {
            //                Session.Character.Family = ServerManager.Instance.FamilyList.FirstOrDefault(s => s.FamilyId == familyId.Value);
            //            }

            //            if (Session.Character.Family != null && Session.Character.FamilyCharacter != null)
            //            {
            //                Session.SendPacket(Session.Character.GenerateGInfo());
            //                Session.SendPackets(Session.Character.GetFamilyHistory());
            //                Session.SendPacket(Session.Character.GenerateFamilyMember());
            //                Session.SendPacket(Session.Character.GenerateFamilyMemberMessage());
            //                Session.SendPacket(Session.Character.GenerateFamilyMemberExp());
            //                if (!string.IsNullOrWhiteSpace(Session.Character.Family.FamilyMessage))
            //                {
            //                    Session.SendPacket(UserInterfaceHelper.Instance.GenerateInfo("--- Family IMessage ---\n" + Session.Character.Family.FamilyMessage));
            //                }
            //            }

            //            IEnumerable<PenaltyLogDTO> warning = _penaltyDao.Where(s => s.AccountId == Session.Character.AccountId).Where(p => p.Penalty == PenaltyType.Warning);
            //            IEnumerable<PenaltyLogDTO> penaltyLogDtos = warning as IList<PenaltyLogDTO> ?? warning.ToList();
            //            if (penaltyLogDtos.Any())
            //            {
            //                Session.SendPacket(UserInterfaceHelper.Instance.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("WARNING_INFO"), penaltyLogDtos.Count())));
            //            }

            //            // finfo - friends info
            var mails = await mailHttpClient.GetMails(-1, session.Player.CharacterId, false).ConfigureAwait(false);
            await mailService.GenerateMailAsync(session, session.Player.CharacterId, mails).ConfigureAwait(false);

            await session.SendPacketAsync(characterPacketSystem.GenerateTitle(session.Player)).ConfigureAwait(false);
            int giftcount = mails.Select(s => s.MailDto).Count(mail => !mail.IsSenderCopy && mail.ReceiverId == session.Player.CharacterId && mail.ItemInstanceId != null && !mail.IsOpened);
            int mailcount = mails.Select(s => s.MailDto).Count(mail => !mail.IsSenderCopy && mail.ReceiverId == session.Player.CharacterId && mail.ItemInstanceId == null && !mail.IsOpened);

            if (giftcount > 0)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Player.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.NewParcelArrived,
                    ArgumentType = 4,
                    Game18NArguments = { giftcount }
                }).ConfigureAwait(false);
            }

            if (mailcount > 0)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Player.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.NewNoteArrived,
                    ArgumentType = 4,
                    Game18NArguments = { mailcount }
                }).ConfigureAwait(false);
            }

            //            Session.Character.DeleteTimeout();

            //            foreach (StaticBuffDTO sb in _staticBuffDao.Where(s => s.CharacterId == Session.Character.CharacterId))
            //            {
            //                Session.Character.AddStaticBuff(sb);
            //            }
            //            if (Session.Character.MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.Act4 || m.MapTypeId == (short)MapTypeEnum.Act42))
            //            {
            //                Session.Character.ConnectAct4();
            //            }
        }
    }
}