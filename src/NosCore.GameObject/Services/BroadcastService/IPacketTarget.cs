//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.Services.BroadcastService
{
    public interface IPacketTarget { }

    public record SessionTarget(string ChannelId) : IPacketTarget;

    public record CharacterTarget(long CharacterId) : IPacketTarget;

    public record MapTarget(Guid MapInstanceId, Func<long, bool>? ExcludeFilter = null) : IPacketTarget;

    public record GroupTarget(long GroupId) : IPacketTarget;

    public record EveryoneTarget(Func<long, bool>? ExcludeFilter = null) : IPacketTarget;

    public static class Targets
    {
        public static SessionTarget Session(string channelId) => new(channelId);
        public static CharacterTarget Character(long characterId) => new(characterId);
        public static MapTarget Map(Guid mapInstanceId, Func<long, bool>? excludeFilter = null) => new(mapInstanceId, excludeFilter);
        public static MapTarget MapExcept(Guid mapInstanceId, long excludeCharacterId) => new(mapInstanceId, id => id == excludeCharacterId);
        public static GroupTarget Group(long groupId) => new(groupId);
        public static EveryoneTarget Everyone(Func<long, bool>? excludeFilter = null) => new(excludeFilter);
    }
}
