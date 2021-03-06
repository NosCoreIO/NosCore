using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NosCore.Core;
using NosCore.Core.HubInterfaces;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.MasterServer.Hubs
{
    public class FriendHub : Hub, IFriendHub
    {
        private readonly ILogger _logger;
        private readonly MasterClientList _masterClientList;

        public FriendHub(ILogger logger, MasterClientList masterClientList)
        {
            _logger = logger;
            _masterClientList = masterClientList;
        }
    }
}
