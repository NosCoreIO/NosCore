//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // n_run 3001 {scriptId}: a story chapter picked from the NPC's "Get Quest" list, asked about first and taken on Yes.
    [UsedImplicitly]
    public sealed class TakeScriptOfferHandler(IQuestService questService) : INrunEventHandler
    {
        public const short Confirmed = 1;

        public NrunRunnerType Runner => NrunRunnerType.GetQuestFromList;

        public bool RequiresNpc => true;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not INonPlayableEntity { NpcMonster: { } npc } entity || packet.Type is not { } scriptId
                || scriptId is < byte.MinValue or > byte.MaxValue)
            {
                return;
            }

            if (packet.Unknown4 == Confirmed)
            {
                await questService.TakeScriptOfferAsync(session.Character, npc.NpcMonsterVNum, (byte)scriptId);
                return;
            }

            if (!questService.CanTakeScriptOffer(session.Character, npc.NpcMonsterVNum, (byte)scriptId))
            {
                return;
            }

            await session.SendPacketAsync(new QnaiPacket
            {
                YesPacket = new NrunPacket
                {
                    Runner = NrunRunnerType.GetQuestFromList,
                    Type = scriptId,
                    VisualType = entity.VisualType,
                    VisualId = entity.VisualId,
                    Unknown4 = Confirmed,
                },
                Question = Game18NConstString.CannotAbandonMainQuest,
            });
        }
    }
}
