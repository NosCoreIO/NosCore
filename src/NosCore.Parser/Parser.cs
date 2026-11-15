//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Hosting;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.Parser
{
    public class Parser(
        ImportFactory factory,
        ILogger<Parser> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage,
        ParserCliOptions cli,
        IHostApplicationLifetime lifetime)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (cli.HasFolder)
                {
                    await RunNonInteractiveAsync(cli.Folder, stoppingToken);
                }
                else
                {
                    await RunInteractiveAsync(stoppingToken);
                }

                logger.LogInformation(logLanguage[LogLanguageKey.DONE]);
            }
            catch (FileNotFoundException)
            {
                logger.LogError(logLanguage[LogLanguageKey.AT_LEAST_ONE_FILE_MISSING]);
                throw;
            }
            finally
            {
                if (cli.HasFolder)
                {
                    lifetime.StopApplication();
                }
            }
        }

        private async Task RunNonInteractiveAsync(string folder, CancellationToken stoppingToken)
        {
            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException($"Folder not found: {folder}");
            }
            logger.LogInformation(logLanguage[LogLanguageKey.PARSING_FOLDER], folder);
            factory.SetFolder(folder);
            await factory.ImportPacketsAsync();
            await RunFullImportAsync();
        }

        private async Task RunInteractiveAsync(CancellationToken stoppingToken)
        {
            logger.LogWarning(logLanguage[LogLanguageKey.ENTER_PATH]);
            var folder = Console.ReadLine();
            var inputRedirected = Console.IsInputRedirected;
            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_ALL]);
            var key = default(ConsoleKeyInfo);
            if (!inputRedirected)
            {
                key = Console.ReadKey(true);
            }

            factory.SetFolder(folder ?? string.Empty);
            await factory.ImportPacketsAsync();

            if (inputRedirected || key.KeyChar != 'n')
            {
                await RunFullImportAsync();
                await Task.Delay(5000, stoppingToken);
                return;
            }

            await RunPromptedImportsAsync();
            await Task.Delay(5000, stoppingToken);
        }

        private async Task RunFullImportAsync()
        {
            await factory.ImportAccountsAsync();
            await factory.ImportMapsAsync();
            await factory.ImportRespawnMapTypeAsync();
            await factory.ImportMapTypeAsync();
            await factory.ImportMapTypeMapAsync();
            await factory.ImportPortalsAsync();
            await factory.ImportScriptedInstancesAsync();
            await factory.ImportI18NAsync();
            await factory.ImportLaddersAsync();
            await factory.ImportItemsAsync();
            await factory.ImportSkillsAsync();
            await factory.ImportCardsAsync();
            await factory.ImportNpcMonstersAsync();
            await factory.ImportDropsAsync();
            await factory.ImportMapNpcsAsync();
            await factory.ImportMapMonstersAsync();
            await factory.ImportShopsAsync();
            await factory.ImportTeleportersAsync();
            await factory.ImportShopItemsAsync();
            await factory.ImportShopSkillsAsync();
            await factory.ImportRecipesAsync();
            await factory.ImportScriptsAsync();
            await factory.ImportQuestsAsync();
        }

        private async Task RunPromptedImportsAsync()
        {
            ConsoleKeyInfo key;

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_MAPS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportMapsAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_MAPTYPES]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n')
            {
                await factory.ImportRespawnMapTypeAsync();
                await factory.ImportMapTypeAsync();
                await factory.ImportMapTypeMapAsync();
            }

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_ACCOUNTS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportAccountsAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_PORTALS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportPortalsAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_I18N]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n')
            {
                await factory.ImportI18NAsync();
                await factory.ImportLaddersAsync();
            }

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_TIMESPACES]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportScriptedInstancesAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_ITEMS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportItemsAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_NPCMONSTERS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportNpcMonstersAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_DROPS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportDropsAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_NPCMONSTERDATA]);
            Console.ReadKey(true);

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_CARDS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportCardsAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_SKILLS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportSkillsAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_MAPNPCS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportMapNpcsAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_MONSTERS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportMapMonstersAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_SHOPS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportShopsAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_TELEPORTERS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportTeleportersAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_SHOPITEMS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportShopItemsAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_SHOPSKILLS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportShopSkillsAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_RECIPES]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportRecipesAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_SCRIPTS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportScriptsAsync();

            logger.LogInformation(logLanguage[LogLanguageKey.ASK_YES_NO], logLanguage[LogLanguageKey.PARSE_QUESTS]);
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportQuestsAsync();
        }
    }
}
