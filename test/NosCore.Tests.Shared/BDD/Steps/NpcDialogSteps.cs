//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Tests.Shared;

namespace NosCore.Tests.Shared.BDD.Steps
{
    public static class NpcDialogSteps
    {
        public const short NpcVNum = 1;

        // Replaces the NPCs of the session's map with these ones.
        public static void WithNpcs(this ClientSession session, params MapNpcDto[] npcs)
        {
            var mapId = session.Character.MapInstance.Map.MapId;
            session.Character.MapInstance.LoadNpcs(
                npcs.Select(s =>
                {
                    s.MapId = mapId;
                    s.VNum = s.VNum == 0 ? NpcVNum : s.VNum;
                    return s;
                }).ToList(),
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = NpcVNum } });
        }

        public static NpcComponentBundle WithNpcAt(this ClientSession session, int mapNpcId, short x, short y)
        {
            session.WithNpcs(new MapNpcDto { MapNpcId = mapNpcId, MapX = x, MapY = y });
            return session.Npc(mapNpcId);
        }

        public static NpcComponentBundle Npc(this ClientSession session, int mapNpcId) =>
            session.Character.MapInstance.GetNpcById(mapNpcId).NotNull();

        public static void WithWindowOpenedAt(this ClientSession session, int mapNpcId, NpcDialogWindow window) =>
            TestHelpers.Instance.NpcDialogService.Open(session.Character, session.Npc(mapNpcId), window);

        public static void WithWindowOpenedRemotely(this ClientSession session, NpcDialogWindow window) =>
            TestHelpers.Instance.NpcDialogService.OpenRemotely(session.Character, window);

        public static void WalkTo(this ClientSession session, short x, short y)
        {
            session.Character.PositionX = x;
            session.Character.PositionY = y;
        }

        public static void IsRegistered(this ClientSession session) =>
            TestHelpers.Instance.SessionRegistry.Register(new SessionInfo
            {
                ChannelId = session.Channel.NotNull().Id,
                SessionId = session.SessionId,
                Sender = session,
                AccountName = session.Account.Name,
                Disconnect = () => System.Threading.Tasks.Task.CompletedTask,
                CharacterId = session.Character.CharacterId,
                MapInstanceId = session.Character.MapInstance.MapInstanceId
            });

        // The dialog stays recorded for the map instance it was opened on.
        public static void WithDialogLeftOnAnotherMap(this ClientSession session) =>
            session.Character.NpcDialog = session.Character.NpcDialog.NotNull() with { MapInstanceId = System.Guid.NewGuid() };
    }
}
