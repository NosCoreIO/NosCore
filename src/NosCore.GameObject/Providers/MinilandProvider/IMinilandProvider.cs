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
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Packets.Enumerations;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.MapInstanceProvider;

namespace NosCore.GameObject.Providers.MinilandProvider
{
    public interface IMinilandProvider
    {
        Miniland GetMiniland(long character);
        void DeleteMiniland(long characterId);
        Miniland Initialize(Character character);
        List<Portal> GetMinilandPortals(long characterId);
        Miniland GetMinilandFromMapInstanceId(Guid mapInstanceId);
        void AddMinilandObject(MapDesignObject mapObject, long characterId, InventoryItemInstance minilandobject);
        Task SetState(long characterId, MinilandState state);
    }
}