//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RemoteCharacterService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.GameObject.Services.ItemGenerationService;

namespace NosCore.PacketHandlers.Command
{
    public class SetGoldCommandPacketHandler(IRemoteCharacterService remoteCharacterService, IItemCatalog itemCatalog)
        : PacketHandler<SetGoldCommandPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SetGoldCommandPacket goldPacket, ClientSession session)
        {
            if (string.IsNullOrEmpty(goldPacket.Name) || goldPacket.Name == session.Character.Name)
            {
                await session.Character.SetGoldAsync(goldPacket.Gold, itemCatalog);
                return;
            }

            if (!await remoteCharacterService.UpdateStatAsync(goldPacket.Name, UpdateStatActionType.UpdateGold, goldPacket.Gold))
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
            }
        }
    }
}
