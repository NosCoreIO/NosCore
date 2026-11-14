//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class SetPlaceOfRevivalHandler(IRespawnService respawnService, INpcDialogService npcDialogService,
        IGameLanguageLocalizer localizer)
        : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.SetPlaceOfRevival;

        public bool RequiresNpc => true;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            var character = session.Character;
            if (target is not NpcComponentBundle npc)
            {
                return;
            }

            // The dialog button and the question's Yes send the same n_run, so the open window tells them apart.
            if (!npcDialogService.IsOpenWith(character, npc, NpcDialogWindow.RevivalQuestion))
            {
                npcDialogService.Open(character, npc, NpcDialogWindow.RevivalQuestion);
                await session.SendPacketAsync(new QnaPacket
                {
                    YesPacket = new NrunPacket
                    {
                        Runner = NrunRunnerType.SetPlaceOfRevival,
                        Type = packet.Type,
                        VisualType = VisualType.Npc,
                        VisualId = npc.VisualId,
                    },
                    Question = localizer[LanguageKey.ASK_CHANGE_RESPAWN_LOCATION, character.AccountLanguage],
                });
                return;
            }

            npcDialogService.Open(character, npc, NpcDialogWindow.Conversation);
            if (!respawnService.SetRespawnPoint(character, character.MapInstance.Map.MapId,
                    npc.PositionX, npc.PositionY))
            {
                return;
            }

            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.RevivalPlaceChanged
            });
        }
    }
}
