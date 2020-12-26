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

using System.Text;
using NosCore.Shared.Enumerations;

namespace NosCore.Core.Extensions
{
    public static class RegionTypeExtension
    {
        public static Encoding? GetEncoding(this RegionType region)
        {
            return region switch
            {
                RegionType.ES => CodePagesEncodingProvider.Instance.GetEncoding(1252),
                RegionType.EN => CodePagesEncodingProvider.Instance.GetEncoding(1252),
                RegionType.FR => CodePagesEncodingProvider.Instance.GetEncoding(1252),
                RegionType.DE => CodePagesEncodingProvider.Instance.GetEncoding(1250),
                RegionType.IT => CodePagesEncodingProvider.Instance.GetEncoding(1250),
                RegionType.PL => CodePagesEncodingProvider.Instance.GetEncoding(1250),
                RegionType.CS => CodePagesEncodingProvider.Instance.GetEncoding(1250),
                RegionType.TR => CodePagesEncodingProvider.Instance.GetEncoding(1254),
                RegionType.RU => CodePagesEncodingProvider.Instance.GetEncoding(1251),
                _ => Encoding.Default
            };
        }
    }
}