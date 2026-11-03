//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public class TimeSpaceEntryService(IScriptedInstanceService scriptedInstanceService,
            IMapChangeService mapChangeService,
            IGameLanguageLocalizer gameLanguageLocalizer)
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
                await RefuseAsync(session, LanguageKey.TIMESPACE_LEVEL_NOT_ALLOWED);
                return;
            }

            if (!character.HoldsRequiredItems(entrance))
            {
                await RefuseAsync(session, LanguageKey.TIMESPACE_REQUIRED_ITEMS_MISSING);
                return;
            }

            var run = await scriptedInstanceService.InstantiateAsync(entrance);
            if (run == null)
            {
                await RefuseAsync(session, LanguageKey.TIMESPACE_NOT_AVAILABLE);
                return;
            }

            await character.TakeRequiredItemsAsync(entrance);

            var firstRoom = run.Rooms[entrance.Definition!.Rooms[0].Key];
            await mapChangeService.ChangeMapInstanceAsync(session, firstRoom,
                entrance.Definition.StartX, entrance.Definition.StartY);
        }

        private Task RefuseAsync(ClientSession session, LanguageKey message)
        {
            return session.SendPacketAsync(session.Character.GenerateSay(
                gameLanguageLocalizer[message, session.Character.AccountLanguage], SayColorType.Yellow));
        }
    }
}
