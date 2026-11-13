//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Core.Encryption;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Shared.Enumerations;

namespace NosCore.Parser.Parsers
{
    // The accounts a fresh database starts with, so a local server can be played without creating one.
    public class AccountParser(IDao<AccountDto, long> accountDao)
    {
        private const string Password = "test";

        public async Task InsertAccountsAsync()
        {
            var password = new Sha512Hasher().Hash(Password);
            foreach (var (name, authority) in new[] { ("admin", AuthorityType.GameMaster), ("test", AuthorityType.User) })
            {
                if (await accountDao.FirstOrDefaultAsync(s => s.Name == name) == null)
                {
                    await accountDao.TryInsertOrUpdateAsync(new AccountDto { Authority = authority, Name = name, Password = password });
                }
            }
        }
    }
}
