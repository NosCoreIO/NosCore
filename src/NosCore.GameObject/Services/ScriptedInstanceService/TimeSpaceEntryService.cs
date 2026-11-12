//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public class TimeSpaceEntryService(IScriptedInstanceService scriptedInstanceService,
            IMapChangeService mapChangeService)
        : ITimeSpaceEntryService
    {
        public Task ShowAsync(ClientSession session, ScriptedInstance entrance)
        {
            return session.SendPacketAsync(entrance.GenerateRbr());
        }

        public async Task EnterAsync(ClientSession session, ScriptedInstance entrance)
        {
            var character = session.Character;

            if (character.Level < entrance.EffectiveLevelMinimum
                || character.Level > entrance.EffectiveLevelMaximum)
            {
                await RefuseAsync(session, Game18NConstString.LevelDoesNotMatch);
                return;
            }

            var missing = character.MissingRequiredItem(entrance);
            if (missing != null)
            {
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.FollowingItemRequired,
                    2, missing.VNum.ToString()));
                return;
            }

            var run = await scriptedInstanceService.InstantiateAsync(entrance);
            if (run == null || entrance.Definition is not { } definition)
            {
                await RefuseAsync(session, Game18NConstString.TimespaceDoesNotExist);
                return;
            }

            await character.TakeRequiredItemsAsync(entrance);

            var firstRoom = run.Rooms[definition.Rooms[0].Key];
            await mapChangeService.ChangeMapInstanceAsync(session, firstRoom,
                definition.StartX, definition.StartY);
        }

        private static Task RefuseAsync(ClientSession session, Game18NConstString message)
        {
            return session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Yellow, message));
        }
    }
}
