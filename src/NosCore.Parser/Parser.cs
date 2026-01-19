//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Hosting;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.Parser
{
    public class Parser(ImportFactory factory, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                logger.Warning(logLanguage[LogLanguageKey.ENTER_PATH]);
                var key = default(ConsoleKeyInfo);

                var folder = Console.ReadLine();
                logger.Information(
                    $"{logLanguage[LogLanguageKey.PARSE_ALL]} [Y/n]");
                key = Console.ReadKey(true);

                factory.SetFolder(folder!);
                await factory.ImportPacketsAsync();

                if (key.KeyChar != 'n')
                {
                    await factory.ImportAccountsAsync();
                    await factory.ImportMapsAsync();
                    await factory.ImportRespawnMapTypeAsync();
                    await factory.ImportMapTypeAsync();
                    await factory.ImportMapTypeMapAsync();
                    await factory.ImportPortalsAsync();
                    await factory.ImportI18NAsync();
                    //factory.ImportScriptedInstances();
                    await factory.ImportItemsAsync();
                    await factory.ImportSkillsAsync();
                    await factory.ImportCardsAsync();
                    await factory.ImportNpcMonstersAsync();
                    await factory.ImportDropsAsync();
                    //factory.ImportNpcMonsterData();
                    await factory.ImportMapNpcsAsync();
                    await factory.ImportMapMonstersAsync();
                    await factory.ImportShopsAsync();
                    //factory.ImportTeleporters();
                    await factory.ImportShopItemsAsync();
                    //factory.ImportShopSkills();
                    //factory.ImportRecipe();
                    await factory.ImportScriptsAsync();
                    await factory.ImportQuestsAsync();
                }
                else
                {
                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_MAPS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportMapsAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_MAPTYPES]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportRespawnMapTypeAsync();
                        await factory.ImportMapTypeAsync();
                        await factory.ImportMapTypeMapAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_ACCOUNTS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportAccountsAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_PORTALS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportPortalsAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_I18N]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportI18NAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_TIMESPACES]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportScriptedInstances();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_ITEMS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportItemsAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_NPCMONSTERS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportNpcMonstersAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_DROPS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportDropsAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_NPCMONSTERDATA]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportNpcMonsterData();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_CARDS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportCardsAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_SKILLS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportSkillsAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_MAPNPCS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportMapNpcsAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_MONSTERS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportMapMonstersAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_SHOPS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportShopsAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_TELEPORTERS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportTeleporters();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_SHOPITEMS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportShopItemsAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_SHOPSKILLS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportShopSkills();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_RECIPES]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportRecipe();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_SCRIPTS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportScriptsAsync();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_QUESTS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportQuestsAsync();
                    }
                }

                logger.Information(logLanguage[LogLanguageKey.DONE]);
                await Task.Delay(5000, stoppingToken);
            }
            catch (FileNotFoundException)
            {
                logger.Error(logLanguage[LogLanguageKey.AT_LEAST_ONE_FILE_MISSING]);
                throw;
            }
        }
    }
}
