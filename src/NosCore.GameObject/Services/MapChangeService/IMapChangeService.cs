using NosCore.GameObject.Networking.ClientSession;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapChangeService
{
    public interface IMapChangeService
    {
        Task ChangeMapInstanceAsync(ClientSession session, Guid mapInstanceId, int? mapX = null, int? mapY = null);
        Task ChangeMapAsync(ClientSession session, short? mapId = null, short? mapX = null, short? mapY = null);
        Task ChangeMapByCharacterIdAsync(long characterId, short mapId, short mapX, short mapY);
    }
}