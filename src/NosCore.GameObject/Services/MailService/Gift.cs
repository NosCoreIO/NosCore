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

namespace NosCore.GameObject.Services.MailService
{
    public class Gift
    {
        public Gift()
        {
        }

        public Gift(short vnum, byte amount, short design = 0, bool isRareRandom = false)
        {
            VNum = vnum;
            Amount = amount;
            IsRareRandom = isRareRandom;
            Design = design;
        }

        public byte Amount { get; set; }

        public short Design { get; set; }

        public short VNum { get; set; }

        public bool IsRareRandom { get; set; }

        public bool IsRandomRare { get; set; }
    }
}
