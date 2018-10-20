//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Parsers

{
    public class MapTypeParser
    {
        internal void InsertMapTypes()
        {
            var list = DaoFactory.MapTypeDao.LoadAll().ToList();
            var mt1 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Act1,
                MapTypeName = "Act1",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt1.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt1);
            }

            var mt2 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Act2,
                MapTypeName = "Act2",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt2.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt2);
            }

            var mt3 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Act3,
                MapTypeName = "Act3",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt3.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt3);
            }

            var mt4 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Act4,
                MapTypeName = "Act4",
                PotionDelay = 5000
            };
            if (list.All(s => s.MapTypeId != mt4.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt4);
            }

            var mt5 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Act51,
                MapTypeName = "Act5.1",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct5,
                ReturnMapTypeId = (long) RespawnType.ReturnAct5
            };
            if (list.All(s => s.MapTypeId != mt5.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt5);
            }

            var mt6 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Act52,
                MapTypeName = "Act5.2",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct5,
                ReturnMapTypeId = (long) RespawnType.ReturnAct5
            };
            if (list.All(s => s.MapTypeId != mt6.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt6);
            }

            var mt7 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Act61,
                MapTypeName = "Act6.1",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct6,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt7.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt7);
            }

            var mt8 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Act62,
                MapTypeName = "Act6.2",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct6,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt8.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt8);
            }

            var mt9 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Act61A,
                MapTypeName = "Act6.1a", // angel camp
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct6,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt9.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt9);
            }

            var mt10 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Act61D,
                MapTypeName = "Act6.1d", // demon camp
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct6,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt10.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt10);
            }

            var mt11 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.CometPlain,
                MapTypeName = "CometPlain",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt11.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt11);
            }

            var mt12 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Mine1,
                MapTypeName = "Mine1",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt12.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt12);
            }

            var mt13 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Mine2,
                MapTypeName = "Mine2",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt13.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt13);
            }

            var mt14 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.MeadowOfMine,
                MapTypeName = "MeadownOfPlain",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt14.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt14);
            }

            var mt15 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.SunnyPlain,
                MapTypeName = "SunnyPlain",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt15.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt15);
            }

            var mt16 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Fernon,
                MapTypeName = "Fernon",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt16.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt16);
            }

            var mt17 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.FernonF,
                MapTypeName = "FernonF",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt17.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt17);
            }

            var mt18 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Cliff,
                MapTypeName = "Cliff",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                ReturnMapTypeId = (long) RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt18.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt18);
            }

            var mt19 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.LandOfTheDead,
                MapTypeName = "LandOfTheDead",
                PotionDelay = 300
            };
            if (list.All(s => s.MapTypeId != mt19.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt19);
            }

            var mt20 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Act32,
                MapTypeName = "Act 3.2",
                PotionDelay = 300
            };
            if (list.All(s => s.MapTypeId != mt20.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt20);
            }

            var mt21 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.CleftOfDarkness,
                MapTypeName = "Cleft of Darkness",
                PotionDelay = 300
            };
            if (list.All(s => s.MapTypeId != mt21.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt21);
            }

            var mt23 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.CitadelAngel,
                MapTypeName = "AngelCitadel",
                PotionDelay = 300
            };
            if (list.All(s => s.MapTypeId != mt23.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt23);
            }

            var mt24 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.CitadelDemon,
                MapTypeName = "DemonCitadel",
                PotionDelay = 300
            };
            if (list.All(s => s.MapTypeId != mt24.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt24);
            }

            var mt25 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Oasis,
                MapTypeName = "Oasis",
                PotionDelay = 300,
                RespawnMapTypeId = (long) RespawnType.DefaultOasis,
                ReturnMapTypeId = (long) RespawnType.DefaultOasis
            };
            if (list.All(s => s.MapTypeId != mt25.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt25);
            }

            var mt26 = new MapTypeDto
            {
                MapTypeId = (short) MapTypeType.Act42,
                MapTypeName = "Act42",
                PotionDelay = 5000
            };
            if (list.All(s => s.MapTypeId != mt26.MapTypeId))
            {
                DaoFactory.MapTypeDao.InsertOrUpdate(ref mt26);
            }

            Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MAPTYPES_PARSED));
        }
    }
}