using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.I18N;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class GroupPacketControllerTests
    {
        private const string ConfigurationPath = "../../../configuration";
        private readonly ClientSession _session = new ClientSession(null, new List<PacketController>() { new GroupPacketController() });
        private AccountDTO _acc;
        private CharacterDTO _chara;
        private GroupPacketController _handler;

        [TestInitialize]
        public void Setup()
        {
        }
    }
}
