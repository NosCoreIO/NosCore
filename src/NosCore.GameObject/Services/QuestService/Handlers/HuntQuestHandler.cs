//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using JetBrains.Annotations;
using NosCore.Packets.Enumerations;
using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.QuestService.Handlers;

[UsedImplicitly]
public sealed class HuntQuestHandler(ILogger<HuntQuestHandler> logger,
    ILogLanguageLocalizer<LogLanguageKey> logLanguage) : KillQuestHandlerBase(logger, logLanguage)
{
    public override QuestType QuestType => QuestType.Hunt;
}
