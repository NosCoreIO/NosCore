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

using System.Linq;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using Serilog;

namespace NosCore.PacketHandlers.Game
{
    public class TitEqPacketHandler : PacketHandler<TitEqPacket>, IWorldPacketHandler
    {
        public override void Execute(TitEqPacket titEqPacket, ClientSession session)
        {
            var tit = session.Character.Titles.FirstOrDefault(s => s.TitleType == titEqPacket.TitleId);
            if (tit != null)
            {
                switch (titEqPacket.Mode)
                {
                    case 1:
                        foreach (var title in session.Character.Titles.Where(s => s.TitleType != titEqPacket.TitleId))
                        {
                            title.Visible = false;
                        }
                        tit.Visible = !tit.Visible;
                        session.SendPacket(new InfoPacket
                        {
                            Message = session.GetMessageFromKey(LanguageKey.TITLE_VISIBILITY_CHANGED)
                        });
                        break;
                    default:
                        foreach (var title in session.Character.Titles.Where(s => s.TitleType != titEqPacket.TitleId))
                        {
                            title.Active = false;
                        }
                        tit.Active = !tit.Active;
                        session.SendPacket(new InfoPacket
                        {
                            Message = session.GetMessageFromKey(LanguageKey.TITLE_EFFECT_CHANGED)
                        });
                        break;
                }
                session.Character.MapInstance.SendPacket(session.Character.GenerateTitInfo());
                session.Character.SendPacket(session.Character.GenerateTitle());
            }
        }
    }
}