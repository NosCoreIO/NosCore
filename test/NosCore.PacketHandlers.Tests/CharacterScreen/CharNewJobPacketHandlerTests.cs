//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class CharNewJobPacketHandlerTests
    {
        private Character Chara = null!;
        private CharNewJobPacketHandler CharNewJobPacketHandler = null!;
        private ClientSession Session = null!;
        private const string TestCharacterName = "TestCharacter";

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Chara = Session.Character;
            await Session.SetCharacterAsync(null);
            TypeAdapterConfig<CharacterDto, Character>.NewConfig().ConstructUsing(src => Chara);
            CharNewJobPacketHandler = new CharNewJobPacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.WorldConfiguration);
        }

        [TestMethod]
        public async Task CreatingMartialArtistWithoutLevel80ShouldFail()
        {
            await new Spec("Creating martial artist without level 80 should fail")
                .WhenAsync(CreatingMartialArtistAsync)
                .ThenAsync(CharacterShouldNotExistAsync)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CreatingMartialArtistWithLevel80ShouldSucceed()
        {
            await new Spec("Creating martial artist with level 80 should succeed")
                .GivenAsync(CharacterIsLevel_Async, 80)
                .WhenAsync(CreatingMartialArtistAsync)
                .ThenAsync(CharacterShouldExistAsync)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CreatingMartialArtistWhenAlreadyOneShouldFail()
        {
            await new Spec("Creating martial artist when already one should fail")
                .GivenAsync(CharacterIsAlreadyMartialArtistAsync)
                .WhenAsync(CreatingMartialArtistAsync)
                .ThenAsync(CharacterShouldNotExistAsync)
                .ExecuteAsync();
        }

        private async Task CharacterIsLevel_Async(int level)
        {
            Chara.Level = (byte)level;
            CharacterDto character = Chara;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(character);
        }

        private async Task CharacterIsAlreadyMartialArtistAsync()
        {
            Chara.Class = CharacterClassType.MartialArtist;
            Chara.Level = 80;
            CharacterDto character = Chara;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(character);
        }

        private async Task CreatingMartialArtistAsync()
        {
            await CharNewJobPacketHandler.ExecuteAsync(new CharNewJobPacket
            {
                Name = TestCharacterName
            }, Session);
        }

        private async Task CharacterShouldNotExistAsync()
        {
            Assert.IsNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == TestCharacterName));
        }

        private async Task CharacterShouldExistAsync()
        {
            Assert.IsNotNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == TestCharacterName));
        }
    }
}
