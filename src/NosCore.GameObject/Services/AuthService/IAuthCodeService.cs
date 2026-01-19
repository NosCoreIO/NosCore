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
        string? GetAccountByAuthCode(string authCode);
        bool TryRemoveAuthCode(string authCode, out string? accountName);

        void MarkReadyForAuth(string accountName, long sessionId);
        bool IsReadyForAuth(string accountName, long sessionId);
        void ClearReadyForAuth(string accountName);
    }
}
