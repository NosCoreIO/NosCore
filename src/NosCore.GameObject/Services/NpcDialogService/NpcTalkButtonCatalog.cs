//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Infastructure;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.NpcDialogService
{
    public interface INpcTalkButtonCatalog
    {
        // Whether the NPC's dialog, or a dialog its buttons open, has a button running the runner,
        // with that argument when one is given.
        bool Offers(NpcComponentBundle npc, NrunRunnerType runner, short? argument = null);
    }

    public class NpcTalkButtonCatalog(List<NpcTalkButtonDto> buttons) : INpcTalkButtonCatalog, ISingletonService
    {
        private readonly ILookup<short, NpcTalkButtonDto> _byDialog = buttons.ToLookup(b => b.DialogId);

        private readonly ConcurrentDictionary<short, IReadOnlyList<NpcTalkButtonDto>> _reachable = new();

        public bool Offers(NpcComponentBundle npc, NrunRunnerType runner, short? argument = null) =>
            npc.Dialog is { } dialog
            && _reachable.GetOrAdd(dialog, Reachable)
                .Any(b => b.Runner == runner && (argument == null || b.Argument == argument));

        private IReadOnlyList<NpcTalkButtonDto> Reachable(short dialog)
        {
            var seen = new HashSet<short> { dialog };
            var pending = new Queue<short>([dialog]);
            var runs = new List<NpcTalkButtonDto>();
            while (pending.TryDequeue(out var current))
            {
                foreach (var button in _byDialog[current])
                {
                    if (button.NextDialogId is { } next)
                    {
                        if (seen.Add(next))
                        {
                            pending.Enqueue(next);
                        }
                    }
                    else
                    {
                        runs.Add(button);
                    }
                }
            }

            return runs;
        }
    }
}
