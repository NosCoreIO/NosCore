namespace NosCore.Tests.PacketHandlerTests
{
    //todo fix
    //[TestClass]
    //public class BlDelPacketHandlerTests
    //{
    //    private BlDelPacketHandler _blDelPacketHandler;
    //    private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
    //    private readonly IGenericDao<CharacterRelationDto> _characterRelationDao = new GenericDao<Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);
    //    private ClientSession _session;
    //    private Mock<IWebApiAccess> _webApiAccess;

    //    [TestInitialize]
    //    public void Setup()
    //    {
    //        Broadcaster.Reset();

    //        _session = TestHelpers.Instance.GenerateSession();
    //        _webApiAccess = new Mock<IWebApiAccess>();
    //        _blDelPacketHandler = new BlDelPacketHandler(_logger, _webApiAccess.Object);
    //    }

    //    [TestMethod]
    //    public void Test_Delete_Blacklist_When_Disconnected()
    //    {
    //        var guid = Guid.NewGuid();
    //        var rel = new CharacterRelationDto
    //        {
    //            CharacterId = _session.Character.CharacterId,
    //            CharacterRelationId = guid,
    //            RelatedCharacterId = 2,
    //            RelationType = CharacterRelationType.Blocked
    //        };
    //        _characterRelationDao.InsertOrUpdate(ref rel);

    //        var bldelPacket = new BlDelPacket
    //        {
    //            CharacterId = 2
    //        };

    //        Assert.IsNotNull(_characterRelationDao.FirstOrDefault(s => _session.Character.CharacterId == s.CharacterId && s.RelatedCharacterId == 2));

    //        _blDelPacketHandler.Execute(bldelPacket, _session);
    //        Assert.IsNull(_characterRelationDao.FirstOrDefault(s => s.RelatedCharacterId == 2));
    //    }

    //    [TestMethod]
    //    public void Test_Delete_Blacklist()
    //    {
    //        var targetSession = TestHelpers.Instance.GenerateSession();
    //        var blinsPacket = new BlInsPacket
    //        {
    //            CharacterId = targetSession.Character.CharacterId
    //        };
    //        new BlInsPackettHandler(_webApiAccess.Object).Execute(blinsPacket, _session);

    //        var bldelPacket = new BlDelPacket
    //        {
    //            CharacterId = targetSession.Character.CharacterId
    //        };
    //        _blDelPacketHandler.Execute(bldelPacket, _session);
    //        Assert.IsNull(_characterRelationDao.FirstOrDefault(s => s.RelatedCharacterId == targetSession.Character.CharacterId));
    //    }
    //}
}
