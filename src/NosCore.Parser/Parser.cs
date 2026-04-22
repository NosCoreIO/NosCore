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
    public class Parser(
        ImportFactory factory,
        ILogger logger,
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
                    await RunNonInteractiveAsync(cli.Folder!, stoppingToken).ConfigureAwait(false);
                }
                else
                {
                    await RunInteractiveAsync(stoppingToken).ConfigureAwait(false);
                }

                logger.Information(logLanguage[LogLanguageKey.DONE]);
            }
            catch (FileNotFoundException)
            {
                logger.Error(logLanguage[LogLanguageKey.AT_LEAST_ONE_FILE_MISSING]);
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
            logger.Information("Parsing non-interactively from {Folder}", folder);
            factory.SetFolder(folder);
            await factory.ImportPacketsAsync().ConfigureAwait(false);
            await RunFullImportAsync().ConfigureAwait(false);
        }

        private async Task RunInteractiveAsync(CancellationToken stoppingToken)
        {
            logger.Warning(logLanguage[LogLanguageKey.ENTER_PATH]);
            var folder = Console.ReadLine();
            var inputRedirected = Console.IsInputRedirected;
            logger.Information(
                $"{logLanguage[LogLanguageKey.PARSE_ALL]} [Y/n]");
            var key = default(ConsoleKeyInfo);
            if (!inputRedirected)
            {
                key = Console.ReadKey(true);
            }

            factory.SetFolder(folder!);
            await factory.ImportPacketsAsync().ConfigureAwait(false);

            if (inputRedirected || key.KeyChar != 'n')
            {
                await RunFullImportAsync().ConfigureAwait(false);
                await Task.Delay(5000, stoppingToken).ConfigureAwait(false);
                return;
            }

            await RunPromptedImportsAsync().ConfigureAwait(false);
            await Task.Delay(5000, stoppingToken).ConfigureAwait(false);
        }

        private async Task RunFullImportAsync()
        {
            await factory.ImportAccountsAsync().ConfigureAwait(false);
            await factory.ImportMapsAsync().ConfigureAwait(false);
            await factory.ImportRespawnMapTypeAsync().ConfigureAwait(false);
            await factory.ImportMapTypeAsync().ConfigureAwait(false);
            await factory.ImportMapTypeMapAsync().ConfigureAwait(false);
            await factory.ImportPortalsAsync().ConfigureAwait(false);
            await factory.ImportI18NAsync().ConfigureAwait(false);
            await factory.ImportItemsAsync().ConfigureAwait(false);
            await factory.ImportSkillsAsync().ConfigureAwait(false);
            await factory.ImportCardsAsync().ConfigureAwait(false);
            await factory.ImportNpcMonstersAsync().ConfigureAwait(false);
            await factory.ImportDropsAsync().ConfigureAwait(false);
            await factory.ImportMapNpcsAsync().ConfigureAwait(false);
            await factory.ImportMapMonstersAsync().ConfigureAwait(false);
            await factory.ImportShopsAsync().ConfigureAwait(false);
            await factory.ImportShopItemsAsync().ConfigureAwait(false);
            await factory.ImportScriptsAsync().ConfigureAwait(false);
            await factory.ImportQuestsAsync().ConfigureAwait(false);
        }

        private async Task RunPromptedImportsAsync()
        {
            ConsoleKeyInfo key;

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_MAPS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportMapsAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_MAPTYPES]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n')
            {
                await factory.ImportRespawnMapTypeAsync().ConfigureAwait(false);
                await factory.ImportMapTypeAsync().ConfigureAwait(false);
                await factory.ImportMapTypeMapAsync().ConfigureAwait(false);
            }

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_ACCOUNTS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportAccountsAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_PORTALS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportPortalsAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_I18N]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportI18NAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_TIMESPACES]} [Y/n]");
            Console.ReadKey(true);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_ITEMS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportItemsAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_NPCMONSTERS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportNpcMonstersAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_DROPS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportDropsAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_NPCMONSTERDATA]} [Y/n]");
            Console.ReadKey(true);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_CARDS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportCardsAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_SKILLS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportSkillsAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_MAPNPCS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportMapNpcsAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_MONSTERS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportMapMonstersAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_SHOPS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportShopsAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_TELEPORTERS]} [Y/n]");
            Console.ReadKey(true);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_SHOPITEMS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportShopItemsAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_SHOPSKILLS]} [Y/n]");
            Console.ReadKey(true);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_RECIPES]} [Y/n]");
            Console.ReadKey(true);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_SCRIPTS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportScriptsAsync().ConfigureAwait(false);

            logger.Information($"{logLanguage[LogLanguageKey.PARSE_QUESTS]} [Y/n]");
            key = Console.ReadKey(true);
            if (key.KeyChar != 'n') await factory.ImportQuestsAsync().ConfigureAwait(false);
        }
    }
}
