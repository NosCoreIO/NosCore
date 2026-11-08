//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.AuthService
{
    public interface IAuthCodeService
    {
        void StoreAuthCode(string authCode, string accountName);
        string? ConsumeAuthCode(string authCode);

        void MarkReadyForAuth(string accountName, long sessionId);
        WorldEntryGrant? ConsumeReadyForAuth(string accountName, long sessionId);

        void MarkReadyForChannelChange(string accountName, long channelId, bool mfaValidated);
        WorldEntryGrant? ConsumeChannelChange(string accountName, long channelId);

        void RegisterSessionIp(string accountName, string ipAddress);
        void UnregisterSessionIp(string accountName);
        string? GetSessionIp(string accountName);
    }
}
