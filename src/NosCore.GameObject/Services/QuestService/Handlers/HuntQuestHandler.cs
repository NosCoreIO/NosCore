//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using JetBrains.Annotations;
using NosCore.Packets.Enumerations;
using Serilog;

namespace NosCore.GameObject.Services.QuestService.Handlers;

[UsedImplicitly]
public sealed class HuntQuestHandler(ILogger logger) : KillQuestHandlerBase(logger)
{
    public override QuestType QuestType => QuestType.Hunt;
}
