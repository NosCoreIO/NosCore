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

using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.HttpClients.MailHttpClient;
using NosCore.GameObject.HttpClients.PacketHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using System.Linq;

namespace NosCore.PacketHandlers.Game
{
    public class GameStartPacketHandler : PacketHandler<GameStartPacket>, IWorldPacketHandler
    {
        private readonly IBlacklistHttpClient _blacklistHttpClient;
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly IFriendHttpClient _friendHttpClient;
        private readonly IMailHttpClient _mailHttpClient;
        private readonly IPacketHttpClient _packetHttpClient;
        private readonly ISerializer _packetSerializer;
        private readonly WorldConfiguration _worldConfiguration;

        public GameStartPacketHandler(WorldConfiguration worldConfiguration, IFriendHttpClient friendHttpClient,
            IChannelHttpClient channelHttpClient,
            IConnectedAccountHttpClient connectedAccountHttpClient, IBlacklistHttpClient blacklistHttpClient,
            IPacketHttpClient packetHttpClient,
            ISerializer packetSerializer, IMailHttpClient mailHttpClient)
        {
            _worldConfiguration = worldConfiguration;
            _packetSerializer = packetSerializer;
            _blacklistHttpClient = blacklistHttpClient;
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _channelHttpClient = channelHttpClient;
            _friendHttpClient = friendHttpClient;
            _packetHttpClient = packetHttpClient;
            _mailHttpClient = mailHttpClient;
        }

        public override void Execute(GameStartPacket packet, ClientSession session)
        {
            if (session.GameStarted || !session.HasSelectedCharacter)
            {
                // character should have been selected in SelectCharacter
                return;
            }

            session.GameStarted = true;

            if (_worldConfiguration.SceneOnCreate && packet.KeepAliveId == null)
            {
                session.SendPacket(new ScenePacket {SceneId = 40});
            }

            if (_worldConfiguration.WorldInformation)
            {
                session.SendPacket(session.Character.GenerateSay("-------------------[NosCore]---------------",
                    SayColorType.Yellow));
                session.SendPacket(session.Character.GenerateSay("Github : https://github.com/NosCoreIO/NosCore/",
                    SayColorType.Purple));
                session.SendPacket(session.Character.GenerateSay("-----------------------------------------------",
                    SayColorType.Yellow));
            }

            session.Character.LoadSpeed();
            //            Session.Character.LoadSkills();
            session.SendPacket(session.Character.GenerateTit());
            session.SendPacket(session.Character.GenerateSpPoint());
            session.SendPacket(session.Character.GenerateRsfi());
            if (session.Character.Hp <= 0)
            {
                //                ServerManager.Instance.ReviveFirstPosition(Session.Character.CharacterId);
            }
            else
            {
                session.ChangeMap();
            }

            //            Session.SendPacket(Session.Character.GenerateSki());
            //            Session.SendPacket($"fd {Session.Character.Reput} 0 {(int)Session.Character.Dignity} {Math.Abs(Session.Character.GetDignityIco())}");
            session.SendPacket(session.Character.GenerateFd());
            session.SendPacket(session.Character.GenerateStat());
            //            Session.SendPacket("rage 0 250000");
            //            Session.SendPacket("rank_cool 0 0 18000");
            //            SpecialistInstance specialistInstance = Session.Character.Inventory.LoadBySlotAndType<SpecialistInstance>(8, InventoryType.Wear);
            var medal = session.Character.StaticBonusList.FirstOrDefault(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);
            if (medal != null)
            {
                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.LOGIN_MEDAL, session.Account.Language), SayColorType.Green));
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
            session.Character.LoadExpensions();
            session.SendPacket(session.Character.GenerateExts(_worldConfiguration));
            //            Session.SendPacket(Session.Character.GenerateMlinfo());
            session.SendPacket(new PclearPacket());

            //            Session.SendPacket(Session.Character.GeneratePinit());
            //            Session.SendPackets(Session.Character.GeneratePst());

            //            Session.SendPacket("zzim");
            //            Session.SendPacket($"twk 2 {Session.Character.CharacterId} {Session.Account.Name} {Session.Character.Name} shtmxpdlfeoqkr");

            //            // qstlist target sqst bf
            //            Session.SendPacket("act6");
            //            Session.SendPacket(Session.Character.GenerateFaction());
            //            // MATES
            //            Session.SendPackets(Session.Character.GenerateScP());
            //            Session.SendPackets(Session.Character.GenerateScN());
            //            Session.Character.GenerateStartupInventory();

            session.SendPacket(session.Character.GenerateGold());
            session.SendPacket(session.Character.GenerateCond());
            session.SendPackets(session.Character.GenerateQuicklist());

            //            string clinit = ServerManager.Instance.TopComplimented.Aggregate("clinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Compliment}|{character.Name}");
            //            string flinit = ServerManager.Instance.TopReputation.Aggregate("flinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Reput}|{character.Name}");
            //            string kdlinit = ServerManager.Instance.TopPoints.Aggregate("kdlinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Act4Points}|{character.Name}");

            //            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateGidx());

            session.Character.SendFinfo(_friendHttpClient, _packetHttpClient, _packetSerializer, true);

            session.SendPacket(session.Character.GenerateFinit(_friendHttpClient, _channelHttpClient,
                _connectedAccountHttpClient));
            session.SendPacket(session.Character.GenerateBlinit(_blacklistHttpClient));
            //            Session.SendPacket(clinit);
            //            Session.SendPacket(flinit);
            //            Session.SendPacket(kdlinit);

            //            Session.Character.LastPVPRevive = SystemTime.Now;

            //            long? familyId = _familyCharacterDAO.FirstOrDefault(s => s.CharacterId == Session.Character.CharacterId)?.FamilyId;
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
            //                    Session.SendPacket(UserInterfaceHelper.Instance.GenerateInfo("--- Family Message ---\n" + Session.Character.Family.FamilyMessage));
            //                }
            //            }

            //            IEnumerable<PenaltyLogDTO> warning = _penaltyDao.Where(s => s.AccountId == Session.Character.AccountId).Where(p => p.Penalty == PenaltyType.Warning);
            //            IEnumerable<PenaltyLogDTO> penaltyLogDtos = warning as IList<PenaltyLogDTO> ?? warning.ToList();
            //            if (penaltyLogDtos.Any())
            //            {
            //                Session.SendPacket(UserInterfaceHelper.Instance.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("WARNING_INFO"), penaltyLogDtos.Count())));
            //            }

            //            // finfo - friends info
            var mails = _mailHttpClient.GetGifts(session.Character.CharacterId);
            session.Character.GenerateMail(mails);

            session.SendPacket(session.Character.GenerateTitle());
            int giftcount = mails.Select(s=>s.MailDto).Count(mail => !mail.IsSenderCopy && mail.ReceiverId == session.Character.CharacterId && mail.ItemInstanceId != null && !mail.IsOpened);
            int mailcount = mails.Select(s => s.MailDto).Count(mail => !mail.IsSenderCopy && mail.ReceiverId == session.Character.CharacterId && mail.ItemInstanceId == null && !mail.IsOpened);
            if (giftcount > 0)
            {
                session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey(LanguageKey.GIFTED, session.Account.Language), giftcount), SayColorType.Purple));
            }
            if (mailcount > 0)
            {
                session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey(LanguageKey.NEW_MAIL, session.Account.Language), mailcount), SayColorType.Yellow));
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