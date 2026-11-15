//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.I18N;
using NosCore.Packets.Interfaces;
using NosCore.Parser.Parsers;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser
{
    public class ImportFactory(CardParser cardParser, DropParser dropParser, ItemParser itemParser,
        MapMonsterParser mapMonsterParser,
        MapNpcParser mapNpcParser, MapParser mapParser, MapTypeMapParser mapTypeMapParser,
        MapTypeParser mapTypeParser, NpcMonsterParser npcMonsterParser,
        PortalParser portalParser, RespawnMapTypeParser respawnMapTypeParser,
        ShopItemParser shopItemParser, ShopSkillParser shopSkillParser, FamilyUpgradeParser familyUpgradeParser, MinigameParser minigameParser, BoxContentParser boxContentParser, MountScrollParser mountScrollParser, NpcExchangeParser npcExchangeParser, SpeedBoostParser speedBoostParser, MountMapSpeedParser mountMapSpeedParser, ItemSummonParser itemSummonParser, ShopParser shopParser, ShopTypeServiceParser shopTypeServiceParser, TeleporterParser teleporterParser, RecipeParser recipeParser, SkillParser skillParser, NpcTalkParser npcTalkParser,
        QuestPrizeParser questPrizeParser, QuestParser questParser, FamilyQuestParser familyQuestParser, ActParser actParser, ScriptParser scriptParser, ScriptOfferParser scriptOfferParser, ChatterLineParser chatterLineParser,
        ConstStringParser constStringParser, ScriptedInstanceParser scriptedInstanceParser, InstantCombatParser instantCombatParser,
        AccountParser accountParser, IDao<I18NQuestDto, int> i18NQuestDao, IDao<I18NSkillDto, int> i18NSkillDao,
        IDao<I18NNpcMonsterTalkDto, int> i18NNpcMonsterTalkDao,
        IDao<I18NNpcMonsterDto, int> i18NNpcMonsterDao, IDao<I18NMapPointDataDto, int> i18NMapPointDataDao,
        IDao<I18NMapIdDataDto, int> i18NMapIdDataDao,
        IDao<I18NItemDto, int> i18NItemDao, IDao<I18NBCardDto, int> i18NbCardDao,
        IDao<I18NCardDto, int> i18NCardDao, IDao<I18NActDescDto, int> i18NActDescDao, PacketTrace packetTrace, ILoggerFactory loggerFactory, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private List<IPacket> _packets = [];
        private string _folder = "";

        public Task ImportAccountsAsync()
        {
            return accountParser.InsertAccountsAsync();
        }

        public Task ImportCardsAsync()
        {
            return cardParser.InsertCardsAsync(_folder);
        }

        public async Task ImportMapNpcsAsync()
        {
            await npcTalkParser.ParseAsync(_folder);
            await mapNpcParser.InsertMapNpcsAsync(_packets);
        }

        public Task ImportMapMonstersAsync()
        {
            return mapMonsterParser.InsertMapMonsterAsync(_packets);
        }

        public async Task ImportShopsAsync()
        {
            await shopParser.InsertShopsAsync(_packets);
            await shopTypeServiceParser.InsertShopTypeServicesAsync(_folder);
        }

        public Task ImportTeleportersAsync()
        {
            return teleporterParser.InsertTeleportersAsync(_packets);
        }

        public async Task ImportShopItemsAsync()
        {
            await shopItemParser.InsertShopItemsAsync(_packets);
            await familyUpgradeParser.InsertFamilyUpgradesAsync(_folder, _packets);
        }

        public Task ImportShopSkillsAsync()
        {
            return shopSkillParser.InsertShopSkillsAsync(_packets);
        }

        public Task ImportRecipesAsync()
        {
            return recipeParser.InsertRecipesAsync(_packets);
        }

        public Task ImportMapsAsync()
        {
            return mapParser.InsertOrUpdateMapsAsync(_folder, _packets);
        }

        public async Task ImportScriptsAsync()
        {
            await scriptParser.InsertScriptsAsync(_folder);
            await scriptOfferParser.InsertScriptOffersAsync(_folder);
        }

        public async Task ImportQuestsAsync()
        {
            await actParser.ImportActAsync(_folder);
            await questPrizeParser.ImportQuestPrizesAsync(_folder);
            await questParser.ImportQuestsAsync(_folder);
            await familyQuestParser.InsertFamilyQuestsAsync(_folder);
        }

        public Task ImportMapTypeAsync()
        {
            return mapTypeParser.InsertMapTypesAsync();
        }

        public Task ImportLaddersAsync()
        {
            return constStringParser.InsertLaddersAsync(_folder);
        }

        public Task ImportMapTypeMapAsync()
        {
            return mapTypeMapParser.InsertMapTypeMapsAsync();
        }

        public async Task ImportNpcMonstersAsync()
        {
            await npcMonsterParser.InsertNpcMonstersAsync(_folder);
            await chatterLineParser.InsertChatterLinesAsync();
            await itemSummonParser.InsertItemSummonsAsync();
        }

        public Task ImportRespawnMapTypeAsync()
        {
            return respawnMapTypeParser.InsertRespawnMapTypeAsync();
        }

        // packet.txt carries no date, so it ranks below every dated capture log beside it.
        public async Task ImportPacketsAsync()
        {
            var sources = new List<CapturedSession>();
            var filePacket = $"{_folder}{Path.DirectorySeparatorChar}packet.txt";
            if (File.Exists(filePacket))
            {
                sources.Add(new CapturedSession(DateTime.MinValue, await packetTrace.ReadAsync(filePacket)));
            }

            foreach (var file in Directory.EnumerateFiles(_folder, "*.txt").Where(f => f != filePacket).Order(StringComparer.Ordinal))
            {
                sources.AddRange(await packetTrace.ReadCaptureLogAsync(file));
            }

            if (sources.Count == 0)
            {
                loggerFactory.CreateLogger<ImportFactory>().LogWarning(logLanguage[LogLanguageKey.PACKET_CAPTURE_MISSING], filePacket);
                return;
            }

            _packets = CaptureSessions.Combine(sources.OrderBy(s => s.Start).Select(s => s.Packets).ToList());
        }

        public Task ImportSkillsAsync()
        {
            return skillParser.InsertSkillsAsync(_folder);
        }

        public async Task ImportDropsAsync()
        {
            await dropParser.InsertDropAsync();
            await instantCombatParser.InsertInstantCombatWavesAsync();
        }

        public Task ImportPortalsAsync()
        {
            return portalParser.InsertPortalsAsync(_packets);
        }

        public Task ImportScriptedInstancesAsync()
        {
            return scriptedInstanceParser.InsertScriptedInstancesAsync(_packets);
        }

        public async Task ImportI18NAsync()
        {
            await new I18NParser<I18NActDescDto, int>(i18NActDescDao, loggerFactory.CreateLogger<I18NParser<I18NActDescDto, int>>(), logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_act_desc.txt", LogLanguageKey.I18N_ACTDESC_PARSED);
            await new I18NParser<I18NBCardDto, int>(i18NbCardDao, loggerFactory.CreateLogger<I18NParser<I18NBCardDto, int>>(), logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_BCard.txt", LogLanguageKey.I18N_BCARD_PARSED);
            await new I18NParser<I18NCardDto, int>(i18NCardDao, loggerFactory.CreateLogger<I18NParser<I18NCardDto, int>>(), logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_Card.txt", LogLanguageKey.I18N_CARD_PARSED);
            await new I18NParser<I18NItemDto, int>(i18NItemDao, loggerFactory.CreateLogger<I18NParser<I18NItemDto, int>>(), logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_Item.txt", LogLanguageKey.I18N_ITEM_PARSED);
            await new I18NParser<I18NMapIdDataDto, int>(i18NMapIdDataDao, loggerFactory.CreateLogger<I18NParser<I18NMapIdDataDto, int>>(), logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_MapIDData.txt", LogLanguageKey.I18N_MAPIDDATA_PARSED);
            await new I18NParser<I18NMapPointDataDto, int>(i18NMapPointDataDao, loggerFactory.CreateLogger<I18NParser<I18NMapPointDataDto, int>>(), logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_MapPointData.txt", LogLanguageKey.I18N_MAPPOINTDATA_PARSED);
            await new I18NParser<I18NNpcMonsterDto, int>(i18NNpcMonsterDao, loggerFactory.CreateLogger<I18NParser<I18NNpcMonsterDto, int>>(), logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_monster.txt", LogLanguageKey.I18N_MPCMONSTER_PARSED);
            await new I18NParser<I18NQuestDto, int>(i18NQuestDao, loggerFactory.CreateLogger<I18NParser<I18NQuestDto, int>>(), logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_quest.txt", LogLanguageKey.I18N_QUEST_PARSED);
            await new I18NParser<I18NSkillDto, int>(i18NSkillDao, loggerFactory.CreateLogger<I18NParser<I18NSkillDto, int>>(), logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_Skill.txt", LogLanguageKey.I18N_SKILL_PARSED);
            await new I18NParser<I18NNpcMonsterTalkDto, int>(i18NNpcMonsterTalkDao, loggerFactory.CreateLogger<I18NParser<I18NNpcMonsterTalkDto, int>>(), logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_npctalk.txt", LogLanguageKey.I18N_NPCMONSTERTALK_PARSED);
        }

        public async Task ImportItemsAsync()
        {
            await itemParser.ParseAsync(_folder);
            await minigameParser.InsertMinigamesAsync();
            await boxContentParser.InsertBoxContentsAsync();
            await mountScrollParser.InsertMountScrollsAsync();
            await npcExchangeParser.InsertNpcExchangesAsync();
            await speedBoostParser.InsertSpeedBoostsAsync();
            await mountMapSpeedParser.InsertMountMapSpeedsAsync();
        }

        public void SetFolder(string folder)
        {
            _folder = folder;
        }
    }
}
