//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using Moq;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.ScriptedInstanceService;
using System;
using System.Collections.Generic;
using System.Linq;
using ServiceUnderTest = NosCore.GameObject.Services.ScriptedInstanceService.ScriptedInstanceService;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class ScriptedInstanceDefinitionParserTests
    {
        private const string FullScript = """
            <Definition>
              <Globals>
                <Id Value="3" />
                <Label Value="Cuby" />
                <Title Value="Mother Cuby" />
                <LevelMinimum Value="20" />
                <LevelMaximum Value="45" />
                <Lives Value="3" />
                <StartX Value="12" />
                <StartY Value="34" />
                <Gold Value="15000" />
                <Reputation Value="200" />
                <Fxp Value="50" />
                <RequieredItems>
                  <Item VNum="1000" Amount="2" />
                </RequieredItems>
                <DrawItems>
                  <Item VNum="1012" Amount="3" Design="7" IsRandomRare="true" />
                  <Item VNum="1013" Amount="1" />
                </DrawItems>
                <SpecialItems>
                  <Item VNum="2282" Amount="1" IsHeroic="true" />
                </SpecialItems>
                <GiftItems>
                  <Item VNum="1030" Amount="5" />
                </GiftItems>
              </Globals>
              <InstanceEvents>
                <CreateMap Map="1" VNum="2004" IndexX="0" IndexY="0">
                  <SummonMonster VNum="334" PositionX="10" PositionY="10" />
                </CreateMap>
                <CreateMap Map="2" VNum="2005" IndexX="1" IndexY="0" />
              </InstanceEvents>
            </Definition>
            """;

        [TestMethod]
        public void TheGlobalsAreReadOffTheValueAttribute()
        {
            var definition = ScriptedInstanceDefinitionParser.Parse(FullScript)!;

            Assert.AreEqual(3, definition.Id);
            Assert.AreEqual("Cuby", definition.Label);
            Assert.AreEqual("Mother Cuby", definition.Title);
            Assert.AreEqual(20, definition.LevelMinimum);
            Assert.AreEqual(45, definition.LevelMaximum);
            Assert.AreEqual(3, definition.Lives);
            Assert.AreEqual(12, definition.StartX);
            Assert.AreEqual(34, definition.StartY);
            Assert.AreEqual(15000L, definition.Gold);
            Assert.AreEqual(200, definition.Reputation);
            Assert.AreEqual(50, definition.FamilyExperience);
        }

        [TestMethod]
        public void EachRewardListIsReadIntoItsOwnPlace()
        {
            var definition = ScriptedInstanceDefinitionParser.Parse(FullScript)!;

            Assert.AreEqual(1000, definition.RequiredItems.Single().VNum);
            Assert.AreEqual(2, definition.DrawItems.Count);
            Assert.AreEqual(2282, definition.SpecialItems.Single().VNum);
            Assert.AreEqual(1030, definition.GiftItems.Single().VNum);
        }

        [TestMethod]
        public void AGiftKeepsItsRarityFlags()
        {
            var drawn = ScriptedInstanceDefinitionParser.Parse(FullScript)!.DrawItems[0];

            Assert.AreEqual(1012, drawn.VNum);
            Assert.AreEqual(3, drawn.Amount);
            Assert.AreEqual(7, drawn.Design);
            Assert.IsTrue(drawn.IsRandomRare);
            Assert.IsFalse(drawn.IsHeroic);
            Assert.IsTrue(ScriptedInstanceDefinitionParser.Parse(FullScript)!.SpecialItems[0].IsHeroic);
        }

        [TestMethod]
        public void TheRoomsKeepTheKeyTheRestOfTheScriptRefersToThemBy()
        {
            var rooms = ScriptedInstanceDefinitionParser.Parse(FullScript)!.Rooms;

            Assert.AreEqual(2, rooms.Count);
            Assert.AreEqual(1, rooms[0].Key);
            Assert.AreEqual(2004, rooms[0].VNum);
            Assert.AreEqual(1, rooms[1].IndexX);
            Assert.AreEqual(0, rooms[1].IndexY);
        }

        [TestMethod]
        public void AScriptThatLeavesOutTheOptionalPartsStillLoads()
        {
            var definition = ScriptedInstanceDefinitionParser.Parse("""
                <Definition>
                  <Globals>
                    <LevelMinimum Value="1" />
                  </Globals>
                </Definition>
                """)!;

            Assert.AreEqual(1, definition.LevelMinimum);
            Assert.AreEqual(0, definition.Reputation);
            Assert.IsNull(definition.Title);
            Assert.AreEqual(0, definition.DrawItems.Count);
            Assert.AreEqual(0, definition.Rooms.Count);
        }

        [TestMethod]
        public void ARowWithNoScriptHasNoDefinition()
        {
            Assert.IsNull(ScriptedInstanceDefinitionParser.Parse(null));
            Assert.IsNull(ScriptedInstanceDefinitionParser.Parse("   "));
        }

        [TestMethod]
        public void BrokenXmlIsNotSwallowed()
        {
            Assert.ThrowsExactly<FormatException>(() =>
                ScriptedInstanceDefinitionParser.Parse("<Definition><Globals><Id Value=\"twelve\" /></Globals></Definition>"));
            Assert.ThrowsExactly<FormatException>(() =>
                ScriptedInstanceDefinitionParser.Parse("<NotADefinition />"));
        }

        [TestMethod]
        public void AnUnreadableScriptLeavesTheDoorStandingWithoutOne()
        {
            var service = new ServiceUnderTest([
                new ScriptedInstanceDto
                {
                    ScriptedInstanceId = 1, MapId = 1, PositionX = 5, PositionY = 6,
                    Type = ScriptedInstanceType.TimeSpace, Script = "<Definition><Globals>"
                }
            ], [], new Mock<IMapInstanceGeneratorService>().Object, new MapInstanceRegistry(), NodaTime.SystemClock.Instance, NullLogger<ServiceUnderTest>.Instance);

            var entrance = service.GetAt(1, 5, 6);

            Assert.IsNotNull(entrance);
            Assert.IsNull(entrance.Definition);
        }

        [TestMethod]
        public void TheScriptsLevelRangeWinsOverTheImportedOne()
        {
            var service = new ServiceUnderTest([
                new ScriptedInstanceDto
                {
                    ScriptedInstanceId = 1, MapId = 1, PositionX = 5, PositionY = 6,
                    Type = ScriptedInstanceType.TimeSpace, LevelMinimum = 10, LevelMaximum = 99,
                    Script = """
                        <Definition><Globals>
                          <LevelMinimum Value="55" /><LevelMaximum Value="60" />
                        </Globals></Definition>
                        """
                }
            ], [], new Mock<IMapInstanceGeneratorService>().Object, new MapInstanceRegistry(), NodaTime.SystemClock.Instance, NullLogger<ServiceUnderTest>.Instance);

            var entrance = service.GetAt(1, 5, 6)!;

            Assert.AreEqual(55, entrance.EffectiveLevelMinimum);
            Assert.AreEqual(60, entrance.EffectiveLevelMaximum);
        }

        [TestMethod]
        public void WithoutAScriptTheImportedLevelRangeIsWhatThereIs()
        {
            var service = new ServiceUnderTest([
                new ScriptedInstanceDto
                {
                    ScriptedInstanceId = 1, MapId = 1, PositionX = 5, PositionY = 6,
                    Type = ScriptedInstanceType.TimeSpace, LevelMinimum = 10, LevelMaximum = 99
                }
            ], [], new Mock<IMapInstanceGeneratorService>().Object, new MapInstanceRegistry(), NodaTime.SystemClock.Instance, NullLogger<ServiceUnderTest>.Instance);

            var entrance = service.GetAt(1, 5, 6)!;

            Assert.AreEqual(10, entrance.EffectiveLevelMinimum);
            Assert.AreEqual(99, entrance.EffectiveLevelMaximum);
        }

        [TestMethod]
        public void TheEntryPanelPadsEveryRewardRowToTheWidthTheClientExpects()
        {
            var instance = new GameObject.Services.ScriptedInstanceService.ScriptedInstance
            {
                Definition = ScriptedInstanceDefinitionParser.Parse(FullScript)
            };

            var packet = instance.GenerateRbr();

            Assert.AreEqual(5, packet.DrawRewards!.Count);
            Assert.AreEqual(2, packet.SpecialRewards!.Count);
            Assert.AreEqual(3, packet.BonusRewards!.Count);
            Assert.AreEqual<short?>(1012, packet.DrawRewards[0]!.ItemId);
            Assert.IsNull(packet.DrawRewards[4]!.ItemId, "an empty slot is a null id, which the serializer writes as -1");
        }

        [TestMethod]
        public void TheEntryPanelChargesOnceForEveryUnitAsked()
        {
            var instance = new GameObject.Services.ScriptedInstanceService.ScriptedInstance
            {
                Definition = ScriptedInstanceDefinitionParser.Parse(FullScript)
            };

            Assert.AreEqual(2, instance.GenerateRbr().RequiredSeeds);
        }

        [TestMethod]
        public void TheEntryPanelNeverClaimsProgressNobodyRecorded()
        {
            var packet = new GameObject.Services.ScriptedInstanceService.ScriptedInstance
            {
                Definition = ScriptedInstanceDefinitionParser.Parse(FullScript)
            }.GenerateRbr();

            Assert.IsFalse(packet.Completed);
            Assert.AreEqual(0, packet.HighScore!.Score);
            Assert.IsNull(packet.HighScore.Nickname);
        }

        [TestMethod]
        public void AnEntranceWithNoScriptStillProducesAPanel()
        {
            var packet = new GameObject.Services.ScriptedInstanceService.ScriptedInstance
            {
                LevelMinimum = 30,
                LevelMaximum = 55
            }.GenerateRbr();

            Assert.AreEqual(30, packet.MinMaxLevel!.MinLevel);
            Assert.AreEqual(55, packet.MinMaxLevel.MaxLevel);
            Assert.AreEqual(5, packet.DrawRewards!.Count);
        }

        [TestMethod]
        public void AHeroTimeSpaceSaysSoOnItsPanel()
        {
            Assert.AreEqual(NosCore.Packets.Enumerations.RbrPacketTsType.HeroMission,
                new GameObject.Services.ScriptedInstanceService.ScriptedInstance { IsHeroic = true }
                    .GenerateRbr().TsBasicInfo!.TsType);
            Assert.AreEqual(NosCore.Packets.Enumerations.RbrPacketTsType.MainMission,
                new GameObject.Services.ScriptedInstanceService.ScriptedInstance()
                    .GenerateRbr().TsBasicInfo!.TsType);
        }
    }
}
