//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

#pragma warning disable 618 //TODO drop obsolete usage

namespace NosCore.GameObject.Messaging.Handlers.Map
{
    [UsedImplicitly]
    public sealed class MinilandEntranceHandler(IMinilandService minilandProvider)
    {
        [UsedImplicitly]
        public async Task Handle(MapInstanceEnteredEvent evt)
        {
            var miniland = minilandProvider.GetMinilandFromMapInstanceId(evt.MapInstance.MapInstanceId);
            if (miniland == null)
            {
                return;
            }

            var session = evt.ClientSession;
            if (miniland.OwnerId != session.Character.CharacterId)
            {
                await session.SendPacketAsync(new MsgPacket
                {
                    Message = miniland.MinilandMessage!.Replace(' ', '^')
                });

                miniland.DailyVisitCount++;
                miniland.VisitCount++;
                await session.SendPacketAsync(miniland.GenerateMlinfobr());
            }
            else
            {
                await session.SendPacketAsync(miniland.GenerateMlinfo());
                await session.SendPacketAsync(session.Character.GenerateMlobjlst());
            }

            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = session.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.TotalVisitors,
                ArgumentType = 4,
                Game18NArguments = { miniland.VisitCount }
            });

            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = session.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.TodayVisitors,
                ArgumentType = 4,
                Game18NArguments = { miniland.DailyVisitCount }
            });
        }
    }
}
