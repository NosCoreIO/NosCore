//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.Tests.Shared.AutoFixture
{
    public class NosCoreFixture : Fixture
    {
        public NosCoreFixture()
        {
            Customize(new AutoMoqCustomization { ConfigureMembers = true });

            this.Inject<IClock>(new FakeClock(Instant.FromUtc(2021, 01, 01, 01, 01, 01)));

            var logLocalizer = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            logLocalizer.Setup(x => x[It.IsAny<LogLanguageKey>()])
                .Returns((LogLanguageKey x) => new LocalizedString(x.ToString(), x.ToString(), false));
            this.Inject(logLocalizer.Object);

            var gameLocalizer = new Mock<IGameLanguageLocalizer>();
            gameLocalizer.Setup(x => x[It.IsAny<LanguageKey>(), It.IsAny<RegionType>()])
                .Returns((LanguageKey k, RegionType r) => new LocalizedString($"{k}{r}", $"{k}{r}", false));
            this.Inject(gameLocalizer.Object);

            this.Inject(Options.Create(new WorldConfiguration
            {
                BackpackSize = 2,
                MaxItemAmount = 999,
                MaxSpPoints = 10_000,
                MaxAdditionalSpPoints = 1_000_000,
                MaxGoldAmount = 999_999_999
            }));

            Customize<ItemDto>(c => c
                .With(x => x.VNum, () => (short)(this.Create<int>() % 1000 + 1000))
                .With(x => x.Type, NoscorePocketType.Main)
                .With(x => x.IsSoldable, () => true)
                .With(x => x.IsDroppable, () => true));

            Customize<BazaarRequest>(c => c
                .With(x => x.Amount, 1)
                .With(x => x.Price, 1000)
                .With(x => x.Duration, 3600)
                .With(x => x.HasMedal, () => false)
                .With(x => x.IsPackage, () => false));

            Customize<CRegPacket>(c => c
                .With(x => x.Type, 0)
                .With(x => x.Inventory, 1)
                .With(x => x.Slot, (short)0)
                .With(x => x.Amount, 1)
                .With(x => x.Price, 1000)
                .With(x => x.Durability, 1)
                .With(x => x.IsPackage, () => false)
                .With(x => x.Taxe, 0)
                .With(x => x.MedalUsed, 0));

            Customize<ItemInstanceDto>(c => c
                .With(x => x.Amount, 1)
                .With(x => x.ItemVNum, (short)1012));
        }
    }
}
