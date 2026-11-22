//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    // The story window shows one button per act the player has reached; each asks for that act's
    // opening scene, which the client plays from scene 40 for act 1 onwards.
    [UsedImplicitly]
    public sealed class ActSceneReplayHandler
    {
        private const byte FirstActScene = 40;

        private static readonly GuriPacketType[] ActButtons =
        [
            GuriPacketType.ReplayAct1Scene,
            GuriPacketType.ReplayAct2Scene,
            GuriPacketType.ReplayAct3Scene,
            GuriPacketType.ReplayAct4Scene,
            GuriPacketType.ReplayAct5Scene,
        ];

        [UsedImplicitly]
        public Task Handle(GuriPacketReceivedEvent evt)
        {
            var act = Array.IndexOf(ActButtons, evt.Packet.Type) + 1;
            var character = evt.ClientSession.Character;
            if (act == 0 || act > character.GenerateRsfi().Act)
            {
                return Task.CompletedTask;
            }

            return evt.ClientSession.SendPacketAsync(new ScenePacket { SceneId = (byte)(FirstActScene + act - 1) });
        }
    }
}
