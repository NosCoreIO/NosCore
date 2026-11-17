//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
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
        public async Task ShowAsync(ClientSession session, ScriptedInstance entrance)
        {
            if (await scriptedInstanceService.LoadAsync(entrance))
            {
                await session.SendPacketAsync(entrance.GenerateRbr());
            }
        }

        public async Task EnterAsync(ClientSession session, ScriptedInstance entrance)
        {
            var character = session.Character;
            if (!await scriptedInstanceService.LoadAsync(entrance))
            {
                await RefuseAsync(session, Game18NConstString.TimespaceDoesNotExist);
                return;
            }

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

        public Task EnterWithStoneAsync(ClientSession session, InventoryItemInstance stone)
        {
            var character = session.Character;
            if (character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
            {
                return RefuseAsync(session, Game18NConstString.CanNotBeUsedHere);
            }

            var item = stone.ItemInstance.Item;
            return EnterAsync(session, new ScriptedInstance
            {
                MapId = character.MapId,
                PositionX = character.PositionX,
                PositionY = character.PositionY,
                Type = ScriptedInstanceType.TimeSpace,
                Script = $"timespace-{item.EffectValue:00}",
                StoneVNum = item.VNum
            });
        }

        private static Task RefuseAsync(ClientSession session, Game18NConstString message)
        {
            return session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Yellow, message));
        }
    }
}
