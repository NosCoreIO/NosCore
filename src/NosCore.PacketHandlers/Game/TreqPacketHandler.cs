//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Core.I18N;
using NosCore.Shared.I18N;
using System;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class TreqPacketHandler(IScriptedInstanceService scriptedInstanceService,
            IMapChangeService mapChangeService,
            IGameLanguageLocalizer gameLanguageLocalizer)
        : PacketHandler<TreqPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(TreqPacket packet, ClientSession session)
        {
            var character = session.Character;

            if (Math.Abs(character.PositionX - packet.X) > 1 || Math.Abs(character.PositionY - packet.Y) > 1)
            {
                return;
            }

            var entrance = scriptedInstanceService.GetAt(character.MapId, (short)packet.X, (short)packet.Y);

            if (entrance == null || entrance.Type != ScriptedInstanceType.TimeSpace)
            {
                return;
            }

            if (packet.StartPress != 1)
            {
                await session.SendPacketAsync(entrance.GenerateRbr()).ConfigureAwait(false);
                return;
            }

            if (character.Level < entrance.EffectiveLevelMinimum
                || character.Level > entrance.EffectiveLevelMaximum)
            {
                await session.SendPacketAsync(session.Character.GenerateSay(
                    gameLanguageLocalizer[LanguageKey.TIMESPACE_LEVEL_NOT_ALLOWED, character.AccountLanguage],
                    SayColorType.Yellow)).ConfigureAwait(false);
                return;
            }

            var run = await scriptedInstanceService.InstantiateAsync(entrance).ConfigureAwait(false);
            if (run == null)
            {
                await session.SendPacketAsync(session.Character.GenerateSay(
                    gameLanguageLocalizer[LanguageKey.TIMESPACE_NOT_AVAILABLE, character.AccountLanguage],
                    SayColorType.Yellow)).ConfigureAwait(false);
                return;
            }

            var firstRoom = run.Rooms[entrance.Definition!.Rooms[0].Key];
            await mapChangeService.ChangeMapInstanceAsync(session, firstRoom,
                entrance.Definition.StartX, entrance.Definition.StartY).ConfigureAwait(false);
        }
    }
}
