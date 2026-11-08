//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SaveService;
using NosCore.GameObject.Services.ShellOptionService;
using NosCore.Shared.I18N;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;

// The channel side of a bazaar operation. A request is retried with the same operation id,
// which the master answers with its first result. Once the master accepted, the channel
// applies the result, saves the character at once and settles; if it cannot apply it, it
// compensates instead.
public static class BazaarHubOperations
{
    private const int Attempts = 3;

    public static async Task<T> RequestAsync<T>(Func<Task<T>> request)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await request();
            }
            catch (Exception) when (attempt < Attempts)
            {
            }
        }
    }

    // Equipment options are stored per instance id, so a wearable handed over under a new id is
    // saved and given a copy of its options before anything else changes. On failure nothing is
    // left behind: deleting the instance removes any option already copied.
    public static async Task<bool> CarryOptionsAsync(IShellOptionService shellOptionService,
        IDao<IItemInstanceDto?, Guid> itemInstanceDao, IItemInstanceDto item, Guid fromInstanceId)
    {
        if (item is not WearableInstance)
        {
            return true;
        }

        try
        {
            if ((await itemInstanceDao.TryInsertOrUpdateAsync(item) != null)
                && await shellOptionService.CopyAsync(fromInstanceId, item.Id))
            {
                return true;
            }
        }
        catch (Exception)
        {
        }

        await DropAsync(itemInstanceDao, item);
        return false;
    }

    public static async Task DropAsync(IDao<IItemInstanceDto?, Guid> itemInstanceDao, IItemInstanceDto item)
    {
        if (item is WearableInstance)
        {
            await itemInstanceDao.TryDeleteAsync(item.Id);
        }
    }

    public static bool CanApply(ClientSession session) =>
        session.HasPlayerEntity && !session.Character.IsDisconnecting;

    // When the master could not be reached at all, a settle or compensation keeps retrying in
    // the background so a master restart does not leave the operation pending. A refusal is the
    // master's final answer and is only logged.
    private static readonly TimeSpan[] BackgroundDelays =
    {
        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(30)
    };

    public static async Task SettleAsync(this IBazaarHub hub, ISaveService saveService, ClientSession session,
        Guid operationId, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        try
        {
            await saveService.SaveAsync(session);
        }
        catch (Exception e)
        {
            logger.LogError(e, logLanguage[LogLanguageKey.BAZAAR_SETTLE_FAILED], operationId);
        }

        await ResolveAsync(() => hub.SettleBazaarAsync(operationId), LogLanguageKey.BAZAAR_SETTLE_FAILED,
            operationId, logger, logLanguage);
    }

    public static Task CompensateAsync(this IBazaarHub hub, Guid operationId, ILogger logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage) =>
        ResolveAsync(() => hub.CompensateBazaarAsync(operationId), LogLanguageKey.BAZAAR_COMPENSATE_FAILED,
            operationId, logger, logLanguage);

    private static async Task ResolveAsync(Func<Task<bool>> request, LogLanguageKey failure, Guid operationId,
        ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        if (await TryAsync(request, failure, operationId, logger, logLanguage))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            foreach (var delay in BackgroundDelays)
            {
                await Task.Delay(delay);
                if (await TryAsync(request, failure, operationId, logger, logLanguage))
                {
                    return;
                }
            }
        });
    }

    // True once the master answered, whatever the answer.
    private static async Task<bool> TryAsync(Func<Task<bool>> request, LogLanguageKey failure, Guid operationId,
        ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        try
        {
            if (!await RequestAsync(request))
            {
                logger.LogError(logLanguage[failure], operationId);
            }

            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, logLanguage[failure], operationId);
            return false;
        }
    }
}
