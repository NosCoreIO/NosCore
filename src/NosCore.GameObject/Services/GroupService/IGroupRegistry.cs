//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.GroupService
{
    public interface IGroupRegistry
    {
        Group? GetById(long groupId);
        void Register(Group group);
        void Unregister(long groupId);
        void RegisterMember(long characterId, long groupId);
        void UnregisterMember(long characterId);
        long GetNextGroupId();
    }
}
