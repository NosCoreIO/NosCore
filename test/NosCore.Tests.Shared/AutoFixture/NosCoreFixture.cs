//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using AutoFixture;
using AutoFixture.AutoMoq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Packets.ClientPackets.Bazaar;

namespace NosCore.Tests.Shared.AutoFixture
{
    public class NosCoreFixture : Fixture
    {
        public NosCoreFixture()
        {
            Customize(new AutoMoqCustomization { ConfigureMembers = true });

            this.Inject(TestHelpers.Instance.Clock);
            this.Inject(TestHelpers.Instance.CharacterDao);
            this.Inject(TestHelpers.Instance.AccountDao);
            this.Inject(TestHelpers.Instance.MateDao);
            this.Inject(TestHelpers.Instance.MinilandDao);
            this.Inject(TestHelpers.Instance.LogLanguageLocalizer);
            this.Inject(TestHelpers.Instance.GameLanguageLocalizer);
            this.Inject(TestHelpers.Instance.WorldConfiguration);

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
