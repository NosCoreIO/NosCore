//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
