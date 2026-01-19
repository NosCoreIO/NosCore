//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Packets.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BroadcastService
{
    public interface ISessionRegistry
    {
        void Register(SessionInfo sessionInfo);
        void Unregister(string channelId);
        void UpdateCharacter(string channelId, long characterId, Guid? mapInstanceId, ICharacterEntity? character);

        IPacketSender? GetSenderByChannelId(string channelId);
        IPacketSender? GetSenderByCharacterId(long characterId);
        IEnumerable<SessionInfo> GetSessionsByMapInstance(Guid mapInstanceId);
        IEnumerable<SessionInfo> GetSessionsByGroup(long groupId);
        IEnumerable<SessionInfo> GetAllSessions();

        ICharacterEntity? GetCharacter(Func<ICharacterEntity, bool> predicate);
        IEnumerable<ICharacterEntity> GetCharacters(Func<ICharacterEntity, bool>? predicate = null);

        Task BroadcastPacketAsync(IPacket packet);
        Task BroadcastPacketAsync(IPacket packet, string excludeChannelId);
        Task DisconnectByCharacterIdAsync(long characterId);

        List<Subscriber> GetConnectedAccounts();
    }

    public class SessionInfo
    {
        public required string ChannelId { get; init; }
        public required long SessionId { get; init; }
        public required IPacketSender Sender { get; init; }
        public required string AccountName { get; init; }
        public required Func<Task> Disconnect { get; init; }

        public long? CharacterId { get; set; }
        public Guid? MapInstanceId { get; set; }
        public long? GroupId { get; set; }
        public ICharacterEntity? Character { get; set; }
    }
}
