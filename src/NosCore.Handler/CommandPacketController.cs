using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Packets.CommandPackets;

namespace NosCore.Controllers
{
    public class CommandPacketController : PacketController
    {
        public CommandPacketController()
        { }

        public void Speed(SpeedPacket speedPacket)
        {
            Session.Character.Speed = (speedPacket.Speed >= 60 ? (byte)59 : speedPacket.Speed);
            Session.SendPacket(Session.Character.GenerateCond());
        }
        
    public void AddMonster(AddMonsterPacket addMonsterPacket)
       {
            if (addMonsterPacket != null)
            {
                if (!Session.HasCurrentMapInstance)
                {
                    return;
                }
                NpcMonster npcmonster = Session.CurrentMapInstance.AddMonster(addMonsterPacket.MonsterVNum);
                if (npcmonster == null)
                {
                    return;
                }
                MapMonsterDTO monst = new MapMonsterDTO
                {
                    MonsterVNum = addMonsterPacket.MonsterVNum,
                    MapY = Session.Character.PositionY,
                    MapX = Session.Character.PositionX,
                    MapId = Session.Character.MapInstance.Map.MapId,
                    Position = (byte)Session.Character.Direction,
                    IsMoving = addMonsterPacket.IsMoving,
                };
                MapMonsterDTO monst1 = monst;
                if (DAOFactory.MapMonsterDAO.FirstOrDefault(s => s.MapMonsterId == monst1.MapMonsterId) == null)
                {
                    DAOFactory.MapMonsterDAO.InsertOrUpdate(ref monst);
                    if (DAOFactory.MapMonsterDAO.FirstOrDefault(s => s.MapMonsterId == monst.MapMonsterId) is MapMonster monster)
                    {
                        monster.Initialize(Session.CurrentMapInstance);
                        Session.CurrentMapInstance.AddMonster(monster);
                        Session.CurrentMapInstance?.Broadcast(monster.GenerateIn());
                    }
                }
               
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(AddMonsterPacket.ReturnHelp(), 10));
            }
        }
