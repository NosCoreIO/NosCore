using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.HubClients
{
    public interface IHubClient
    {
        Task StartAsync(CancellationToken stoppingToken);
    }
}
