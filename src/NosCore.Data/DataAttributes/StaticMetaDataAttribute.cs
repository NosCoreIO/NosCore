//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using System;

namespace NosCore.Data.DataAttributes
{
    public class StaticMetaDataAttribute : Attribute
    {
        public LogLanguageKey LoadedMessage { get; set; }

        public LogLanguageKey EmptyMessage { get; set; }
    }
}
