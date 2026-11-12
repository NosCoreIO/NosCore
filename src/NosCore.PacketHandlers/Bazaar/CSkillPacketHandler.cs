//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CSkillPacketHandler(IClock clock, INpcDialogService npcDialogService) : PacketHandler<CSkillPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CSkillPacket packet, ClientSession clientSession)
        {
            if (clientSession.Character.BazaarMedal != null)
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.AttackWhileBazar
                });

                npcDialogService.OpenRemotely(clientSession.Character, NpcDialogWindow.Bazaar);
                await clientSession.SendPacketAsync(clientSession.Character.GenerateBazaarWindow(clock));
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
