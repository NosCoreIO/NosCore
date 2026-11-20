//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Dao;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.CharacterService
{
    [TestClass]
    public class CharacterLoadServiceTests
    {
        // Fixed when the character is created; a save never writes them.
        private static readonly string[] Identity =
        [
            nameof(CharacterDto.CharacterId), nameof(CharacterDto.AccountId), nameof(CharacterDto.ServerId),
            nameof(CharacterDto.Name), nameof(CharacterDto.Slot), nameof(CharacterDto.Prefix)
        ];

        // No component holds these yet, so they only travel on the row the player keeps.
        private static readonly string[] RowOnly =
        [
            nameof(CharacterDto.Act4Dead), nameof(CharacterDto.Act4Kill), nameof(CharacterDto.Act4Points),
            nameof(CharacterDto.ArenaWinner), nameof(CharacterDto.Elo), nameof(CharacterDto.MasterPoints),
            nameof(CharacterDto.MasterTicket), nameof(CharacterDto.MaxMateCount), nameof(CharacterDto.RagePoint),
            nameof(CharacterDto.ShouldRename), nameof(CharacterDto.State), nameof(CharacterDto.TalentLose),
            nameof(CharacterDto.TalentSurrender), nameof(CharacterDto.TalentWin)
        ];

        private static readonly PropertyInfo[] Columns = typeof(CharacterDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod?.IsPublic == true)
            .ToArray();

        private TestWorld World;
        private ClientSession Session;
        private CharacterDto Stored;
        private CharacterDto? Saved;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.ClearPlayerEntity();

            World.Inject(Dao<Database.Entities.Title, TitleDto, Guid>());
            World.Inject(Dao<Database.Entities.CharacterQuest, CharacterQuestDto, Guid>());
            World.Inject(Dao<Database.Entities.CharacterQuestObjective, CharacterQuestObjectiveDto, Guid>());
            World.Inject(Dao<Database.Entities.Respawn, RespawnDto, long>());
            World.Freeze<Mock<IMinilandService>>().Setup(s => s.GetMiniland(It.IsAny<long>()))
                .Returns(new GameObject.Services.MinilandService.Miniland());
            World.Freeze<Mock<IStaticBuffService>>().Setup(s => s.SaveAsync(It.IsAny<PlayerComponentBundle>()))
                .ReturnsAsync(true);
            var mates = World.Freeze<Mock<IMateService>>();
            mates.Setup(s => s.LoadAsync(It.IsAny<long>())).ReturnsAsync(new List<Mate>());
            mates.Setup(s => s.SaveAsync(It.IsAny<IEnumerable<Mate>>())).ReturnsAsync(true);
        }

        [TestMethod]
        public async Task EveryColumnSurvivesALoadAndASave()
        {
            await new Spec("Every column of a character row comes back unchanged after a load and a save")
                .GivenAsync(ACharacterWithEveryColumnSet)
                .WhenAsync(LoadingAndSavingIt)
                .Then(TheStoredRowShouldBeUnchangedExceptFor_, Array.Empty<string>())
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EveryColumnThePlayerHoldsIsLoadedIntoItsComponents()
        {
            await new Spec("Every column the player holds goes through its components, not only through the row it keeps")
                .GivenAsync(ACharacterWithEveryColumnSet)
                .WhenAsync(LoadingItAndSavingItOntoABlankRow)
                .Then(TheStoredRowShouldBeUnchangedExceptFor_, RowOnly)
                .ExecuteAsync();
        }

        private async Task ACharacterWithEveryColumnSet()
        {
            Stored = new CharacterDto { Name = string.Empty };
            var next = 2;
            foreach (var column in Columns)
            {
                column.SetValue(Stored, NonDefault(column, ref next));
            }

            Stored.CharacterId = 1000;
            Stored.AccountId = Session.Account.AccountId;
            Stored.ServerId = (byte)World.WorldConfiguration.Value.ServerId;
            Stored.Slot = 2;
            Stored.MapId = TestWorld.ShopMapId;
            Stored.Hp = 1;
            Stored.Mp = 1;
            var script = new ScriptDto { Id = Guid.NewGuid() };
            Stored.CurrentScriptId = script.Id;
            await World.CharacterDao.TryInsertOrUpdateAsync(Stored).OrThrowAsync();

            World.Freeze<Mock<IDao<ScriptDto, Guid>>>()
                .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<ScriptDto, bool>>>()))
                .ReturnsAsync(script);
        }

        private async Task LoadingAndSavingIt() => await LoadAndSaveAsync(blankRow: false);

        private async Task LoadingItAndSavingItOntoABlankRow() => await LoadAndSaveAsync(blankRow: true);

        private async Task LoadAndSaveAsync(bool blankRow)
        {
            var row = (await World.CharacterDao.FirstOrDefaultAsync(c => c.CharacterId == Stored.CharacterId)).NotNull();
            var player = await World.Create<CharacterLoadService>().LoadAsync(Session, row);
            if (blankRow)
            {
                var blank = new CharacterDto { Name = string.Empty };
                foreach (var column in Columns.Where(c => Identity.Contains(c.Name)))
                {
                    column.SetValue(blank, column.GetValue(row));
                }

                player.CharacterDto = blank;
            }

            Assert.IsTrue(await World.Create<GameObject.Services.SaveService.SaveService>().SaveAsync(Session));
            Saved = await World.CharacterDao.FirstOrDefaultAsync(c => c.CharacterId == Stored.CharacterId);
        }

        private void TheStoredRowShouldBeUnchangedExceptFor_(string[] excluded)
        {
            var saved = Saved.NotNull();
            var dropped = Columns
                .Where(c => !excluded.Contains(c.Name) && !Equals(c.GetValue(Stored), c.GetValue(saved)))
                .Select(c => $"{c.Name}: stored {c.GetValue(Stored)}, saved {c.GetValue(saved)}")
                .ToList();
            Assert.IsEmpty(dropped, string.Join(Environment.NewLine, dropped));
        }

        private static object NonDefault(PropertyInfo column, ref int next)
        {
            var type = Nullable.GetUnderlyingType(column.PropertyType) ?? column.PropertyType;
            if (type.IsEnum)
            {
                return Enum.GetValues(type).Cast<object>().First(v => Convert.ToInt64(v) != 0);
            }

            return type switch
            {
                _ when type == typeof(bool) => true,
                _ when type == typeof(byte) => (byte)next++,
                _ when type == typeof(short) => (short)next++,
                _ when type == typeof(int) => next++,
                _ when type == typeof(long) => (long)next++,
                _ when type == typeof(string) => column.Name,
                _ when type == typeof(Guid) => Guid.NewGuid(),
                _ => throw new NotSupportedException($"Give {column.Name} ({type.Name}) a value here.")
            };
        }

        private IDao<TDto, TPk> Dao<TEntity, TDto, TPk>() where TEntity : class where TPk : struct =>
            new Dao<TEntity, TDto, TPk>(NullLogger<Dao<TEntity, TDto, TPk>>.Instance, World.DbContextBuilder);
    }
}
