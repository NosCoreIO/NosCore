//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;

//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.GameObject.Services.MapInstanceGenerationService.Handlers
{
    public class MinilandEntranceHandler(IMinilandService minilandProvider) : IMapInstanceEntranceEventHandler
    {
        public bool Condition(MapInstance condition) => minilandProvider.GetMinilandFromMapInstanceId(condition.MapInstanceId) != null;

        public async Task ExecuteAsync(RequestData<MapInstance> requestData)
        {
            var miniland = minilandProvider.GetMinilandFromMapInstanceId(requestData.Data.MapInstanceId)!;
            if (miniland.CharacterEntity!.VisualId != requestData.ClientSession.Character.CharacterId)
            {
                await requestData.ClientSession.SendPacketAsync(new MsgPacket
                {
                    Message = miniland.MinilandMessage!.Replace(' ', '^')
                });

                miniland.DailyVisitCount++;
                miniland.VisitCount++;
                await requestData.ClientSession.SendPacketAsync(miniland.GenerateMlinfobr());
            }
            else
            {
                await requestData.ClientSession.SendPacketAsync(miniland.GenerateMlinfo());
                await requestData.ClientSession.SendPacketAsync(requestData.ClientSession.Character.GenerateMlobjlst());
            }

            //TODO add pets

            await requestData.ClientSession.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = requestData.ClientSession.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.TotalVisitors,
                ArgumentType = 4,
                Game18NArguments = { miniland.VisitCount }
            });

            await requestData.ClientSession.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = requestData.ClientSession.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.TodayVisitors,
                ArgumentType = 4,
                Game18NArguments = { miniland.DailyVisitCount }
            });
        }
    }
}
