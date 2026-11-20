//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Messaging.Handlers.TimeSpace
{
    // Runs after the quests, so a clear that completes one has already restored dignity to the maximum and gains nothing on top.
    [UsedImplicitly]
    [GameEventOrder(1)]
    public sealed class TimeSpaceSucceededHandler(IDao<CompletedTimeSpaceDto, Guid> completedTimeSpaceDao,
        ILogger<TimeSpaceSucceededHandler> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        // A first clear has been seen giving its 100 only once, against several repeats giving 50.
        private const short FirstClearDignity = 100;
        private const short RepeatClearDignity = 50;

        [UsedImplicitly]
        public async Task Handle(ScriptedInstanceSucceededEvent evt)
        {
            if (evt.Type != ScriptedInstanceType.TimeSpace)
            {
                return;
            }

            var character = evt.Character;
            character.ClearTimeSpaceFailures();
            var completed = character.CompletedTimeSpaces ?? ImmutableHashSet<short>.Empty;
            if (completed.Contains(evt.InstanceId))
            {
                await character.GainDignityAsync(RepeatClearDignity);
                return;
            }

            character.CompletedTimeSpaces = completed.Add(evt.InstanceId);
            await character.GainDignityAsync(FirstClearDignity);
            var saved = await completedTimeSpaceDao.TryInsertOrUpdateAsync(new CompletedTimeSpaceDto
            {
                Id = Guid.NewGuid(),
                CharacterId = character.CharacterId,
                TimeSpaceId = evt.InstanceId
            });
            if (saved == null)
            {
                logger.LogError(logLanguage[LogLanguageKey.SAVE_CHARACTER_FAILED], character.CharacterId);
            }
        }
    }
}
