//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CSkillPacketHandler(IClock clock) : PacketHandler<CSkillPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CSkillPacket packet, ClientSession clientSession)
        {
            var medalBonus = clientSession.Character.StaticBonusList.FirstOrDefault(s =>
                (s.StaticBonusType == StaticBonusType.BazaarMedalGold) ||
                (s.StaticBonusType == StaticBonusType.BazaarMedalSilver));
            if (medalBonus != null)
            {
                var medal = medalBonus.StaticBonusType == StaticBonusType.BazaarMedalGold ? (byte)MedalType.Gold : (byte)MedalType.Silver;
                var time = (int)(medalBonus.DateEnd == null ? 720 : (((Instant)medalBonus.DateEnd) - clock.GetCurrentInstant()).TotalHours);
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.AttackWhileBazar
                });

                await clientSession.SendPacketAsync(new WopenPacket
                {
                    Type = WindowType.NosBazaar,
                    Unknown = medal,
                    Unknown2 = (byte)time
                });
            }
            else
            {
                await clientSession.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.NosMerchantMedaleAllowPlayerToUseNosbazarOnAllGeneralMaps
                });
            }
        }
    }
}
