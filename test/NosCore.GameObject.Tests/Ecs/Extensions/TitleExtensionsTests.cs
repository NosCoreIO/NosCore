//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    [TestClass]
    public class TitleExtensionsTests
    {
        private const short TitleId = 1;
        private ClientSession Session;
        private bool Toggled;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session.Character.Titles = new List<TitleDto>
            {
                new TitleDto { TitleType = TitleId, Visible = false, Active = false },
                new TitleDto { TitleType = 2, Visible = true, Active = true }
            };
        }

        [TestMethod]
        public async Task ShowingATitleHidesTheOthers()
        {
            await new Spec("Showing a title hides every other title")
                .When(TogglingTheVisibilityOf_, TitleId)
                .Then(TheToggleShouldApply_, true)
                .And(OnlyTitle_ShouldBeVisible, TitleId)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ActivatingATitleDeactivatesTheOthers()
        {
            await new Spec("Turning a title's effect on turns every other title's effect off")
                .When(TogglingTheEffectOf_, TitleId)
                .Then(TheToggleShouldApply_, true)
                .And(OnlyTitle_ShouldBeActive, TitleId)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TogglingAShownTitleHidesIt()
        {
            await new Spec("Toggling the title already shown hides it")
                .When(TogglingTheVisibilityOf_, (short)2)
                .Then(NoTitleShouldBeVisible)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATitleTheCharacterDoesNotOwnChangesNothing()
        {
            await new Spec("A title the character does not own changes nothing")
                .When(TogglingTheEffectOf_, (short)9999)
                .Then(TheToggleShouldApply_, false)
                .And(OnlyTitle_ShouldBeActive, (short)2)
                .ExecuteAsync();
        }

        private void TogglingTheVisibilityOf_(short titleType) =>
            Toggled = Session.Character.ToggleTitleVisibility(titleType);

        private void TogglingTheEffectOf_(short titleType) =>
            Toggled = Session.Character.ToggleTitleEffect(titleType);

        private void TheToggleShouldApply_(bool applied) => Assert.AreEqual(applied, Toggled);

        private void OnlyTitle_ShouldBeVisible(short titleType) =>
            CollectionAssert.AreEqual(new[] { titleType },
                Session.Character.Titles.Where(t => t.Visible).Select(t => (short)t.TitleType).ToArray());

        private void OnlyTitle_ShouldBeActive(short titleType) =>
            CollectionAssert.AreEqual(new[] { titleType },
                Session.Character.Titles.Where(t => t.Active).Select(t => (short)t.TitleType).ToArray());

        private void NoTitleShouldBeVisible() => Assert.IsFalse(Session.Character.Titles.Any(t => t.Visible));
    }
}
