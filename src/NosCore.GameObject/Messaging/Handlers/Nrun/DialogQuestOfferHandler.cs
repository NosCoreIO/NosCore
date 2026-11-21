//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // A dialog button that hands out one of its NPC's quests at random. A quest of that NPC still
    // held, or done today, stands for all of them, so the NPC gives one a day. A quest that follows
    // another is only offered once that one is done.
    public abstract class DialogQuestOfferHandler(
        IQuestService questService,
        List<QuestDto> quests,
        IRandomProvider random,
        IClock clock,
        INpcTalkButtonCatalog npcTalkButtons) : INrunEventHandler
    {
        public abstract NrunRunnerType Runner { get; }

        public bool RequiresNpc => true;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            var character = session.Character;
            var offers = quests.Where(q => q.OfferedBy == Runner && character.HasDoneRequiredQuest(q)).ToList();
            if (offers.Count == 0 || target is not NpcComponentBundle npc || !npcTalkButtons.Offers(npc, Runner))
            {
                return Task.CompletedTask;
            }

            var now = clock.GetCurrentInstant();
            var taken = character.Quests.Values.FirstOrDefault(q =>
                q.Quest.OfferedBy == Runner && (q.CompletedOn == null || q.IsDoneAt(now)));
            var fitting = offers.Where(q => q.LevelMin <= character.Level && character.Level <= q.LevelMax).ToList();
            var questId = taken?.QuestId
                ?? (fitting.Count == 0 ? offers[0] : fitting[random.Next(0, fitting.Count)]).QuestId;
            return questService.AddQuestAsync(character, QuestActionType.Achieve, questId);
        }
    }
}
