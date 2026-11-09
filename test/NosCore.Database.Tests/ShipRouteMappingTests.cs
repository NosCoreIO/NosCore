using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database.Entities;
using NosCore.Packets.Enumerations;
using SpecLight;

namespace NosCore.Database.Tests;

[TestClass]
public class ShipRouteMappingTests
{
    [TestMethod]
    public void RunnerEnumsUseNumericColumnsWithoutCreatingPostgresEnumLabels() =>
        new Spec("NPC runner enums remain semantic C# types stored as smallint, without PostgreSQL enum label limits")
            .Then(RunnersShouldRemainEnums)
            .And(RunnerColumnsShouldBeNumeric)
            .And(SchemaShouldNotDeclareANativeRunnerEnum)
            .Execute();

    private static NosCoreContext Context() => new(new DbContextOptionsBuilder<NosCoreContext>()
        .UseNpgsql("Host=localhost;Database=unused", options => options.UseNodaTime()).Options);

    private void RunnersShouldRemainEnums()
    {
        Assert.AreEqual(typeof(NrunRunnerType), typeof(ShipRoute).GetProperty(nameof(ShipRoute.Runner))!.PropertyType);
        Assert.AreEqual(typeof(NrunRunnerType), typeof(ShipRoute).GetProperty(nameof(ShipRoute.CancelRunner))!.PropertyType);
    }

    private void RunnerColumnsShouldBeNumeric()
    {
        using var context = Context();
        var route = context.Model.FindEntityType(typeof(ShipRoute))!;
        Assert.AreEqual("smallint", route.FindProperty(nameof(ShipRoute.Runner))!.GetColumnType());
        Assert.AreEqual("smallint", route.FindProperty(nameof(ShipRoute.CancelRunner))!.GetColumnType());
    }

    private void SchemaShouldNotDeclareANativeRunnerEnum()
    {
        using var context = Context();
        Assert.IsFalse(context.Database.GenerateCreateScript().Contains("CREATE TYPE nrun_runner_type"));
    }
}
