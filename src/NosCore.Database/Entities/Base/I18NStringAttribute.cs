//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.Database.Entities.Base
{
    public class I18NStringAttribute(Type type) : Attribute
    {
        public Type Type { get; set; } = type;
    }
}
