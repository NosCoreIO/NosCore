//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;

namespace NosCore.Core
{
    public sealed class SessionFactory
    {
        private static readonly Lazy<SessionFactory> Lazy = new(() => new SessionFactory());

        private SessionFactory()
        {
            AuthCodes = new ConcurrentDictionary<string, string>();
            ReadyForAuth = new ConcurrentDictionary<string, long>();
        }

        public static SessionFactory Instance => Lazy.Value;

        public ConcurrentDictionary<string, string> AuthCodes { get; }
        public ConcurrentDictionary<string, long> ReadyForAuth { get; }
    }
}
