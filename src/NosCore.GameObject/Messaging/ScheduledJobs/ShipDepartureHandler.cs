using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Services.ShipService;

namespace NosCore.GameObject.Messaging.ScheduledJobs;

[UsedImplicitly]
public sealed class ShipDepartureHandler(IShipService ships)
{
    public Task Handle(ShipDepartureMessage _) => ships.DepartAsync();
}
