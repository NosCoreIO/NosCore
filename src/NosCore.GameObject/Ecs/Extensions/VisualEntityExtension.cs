//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;

namespace NosCore.GameObject.Ecs.Extensions
{
    public static class VisualEntityExtension
    {
        public static OutPacket GenerateOut(this IVisualEntity visualEntity) => new()
        {
            VisualType = visualEntity.VisualType,
            VisualId = visualEntity.VisualId
        };

        public static SpeakPacket GenerateSpk(this INamedEntity visualEntity, SpeakPacket packet)
        {
            return new SpeakPacket
            {
                VisualType = visualEntity.VisualType,
                VisualId = visualEntity.VisualId,
                SpeakType = packet.SpeakType,
                EntityName = visualEntity.Name,
                Message = packet.Message
            };
        }
    }
}
