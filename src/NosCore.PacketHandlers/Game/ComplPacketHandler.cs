//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ComplimentService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class ComplPacketHandler(IComplimentService complimentService)
        : PacketHandler<ComplPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ComplPacket packet, ClientSession session)
        {
            var character = session.Character;
            var result = complimentService.Compliment(character, packet.CharacterId);
            switch (result.Outcome)
            {
                case ComplimentOutcome.NoTarget:
                    return;

                case ComplimentOutcome.Complimented:
                    var target = result.Target!.Value;
                    await session.SendPacketAsync(new Sayi2Packet
                    {
                        VisualType = VisualType.Player,
                        VisualId = character.CharacterId,
                        Type = SayColorType.Green,
                        Message = Game18NConstString.HaveCommendedPercentZero,
                        ArgumentType = 1,
                        Game18NArguments = { target.Name }
                    });
                    await target.SendPacketAsync(new Sayi2Packet
                    {
                        VisualType = VisualType.Player,
                        VisualId = target.CharacterId,
                        Type = SayColorType.Green,
                        Message = Game18NConstString.HaveBeenCommendedByPercentZero,
                        ArgumentType = 1,
                        Game18NArguments = { character.Name }
                    });
                    return;

                case ComplimentOutcome.TooSoonAfterConnection:
                    await session.SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = character.CharacterId,
                        Type = SayColorType.Red,
                        Message = Game18NConstString.CantCommendNow,
                        ArgumentType = 4,
                        Game18NArguments = { result.MinutesToWait }
                    });
                    return;

                default:
                    await session.SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = character.CharacterId,
                        Type = SayColorType.Red,
                        Message = result.Outcome switch
                        {
                            ComplimentOutcome.InGlacernon => Game18NConstString.CannotLeadInGlacernon,
                            ComplimentOutcome.LevelTooLow => Game18NConstString.OnlyPlayersAboveLevelThirtyLead,
                            _ => Game18NConstString.CanOnlyLeadOncePerDayAndAccount,
                        }
                    });
                    return;
            }
        }
    }
}
