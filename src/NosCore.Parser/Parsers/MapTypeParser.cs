using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.Shared.Enumerations.Buff;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Parsers

{
    public class MapTypeParser
    {
        internal void InsertMapTypes()
        {
            List<MapTypeDTO> list = DAOFactory.MapTypeDAO.LoadAll().ToList();
            MapTypeDTO mt1 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Act1,
                MapTypeName = "Act1",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct1,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt1.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt1);
            }
            MapTypeDTO mt2 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Act2,
                MapTypeName = "Act2",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct1,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt2.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt2);
            }
            MapTypeDTO mt3 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Act3,
                MapTypeName = "Act3",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct1,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt3.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt3);
            }
            MapTypeDTO mt4 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Act4,
                MapTypeName = "Act4",
                PotionDelay = 5000
            };
            if (list.All(s => s.MapTypeId != mt4.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt4);
            }
            MapTypeDTO mt5 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Act51,
                MapTypeName = "Act5.1",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct5,
                ReturnMapTypeId = (long)RespawnType.ReturnAct5
            };
            if (list.All(s => s.MapTypeId != mt5.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt5);
            }
            MapTypeDTO mt6 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Act52,
                MapTypeName = "Act5.2",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct5,
                ReturnMapTypeId = (long)RespawnType.ReturnAct5
            };
            if (list.All(s => s.MapTypeId != mt6.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt6);
            }
            MapTypeDTO mt7 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Act61,
                MapTypeName = "Act6.1",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct6,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt7.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt7);
            }
            MapTypeDTO mt8 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Act62,
                MapTypeName = "Act6.2",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct6,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt8.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt8);
            }
            MapTypeDTO mt9 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Act61A,
                MapTypeName = "Act6.1a", // angel camp
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct6,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt9.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt9);
            }
            MapTypeDTO mt10 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Act61D,
                MapTypeName = "Act6.1d", // demon camp
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct6,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt10.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt10);
            }
            MapTypeDTO mt11 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.CometPlain,
                MapTypeName = "CometPlain",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct1,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt11.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt11);
            }
            MapTypeDTO mt12 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Mine1,
                MapTypeName = "Mine1",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct1,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt12.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt12);
            }
            MapTypeDTO mt13 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Mine2,
                MapTypeName = "Mine2",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct1,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt13.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt13);
            }
            MapTypeDTO mt14 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.MeadowOfMine,
                MapTypeName = "MeadownOfPlain",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct1,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt14.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt14);
            }
            MapTypeDTO mt15 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.SunnyPlain,
                MapTypeName = "SunnyPlain",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct1,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt15.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt15);
            }
            MapTypeDTO mt16 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Fernon,
                MapTypeName = "Fernon",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct1,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt16.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt16);
            }
            MapTypeDTO mt17 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.FernonF,
                MapTypeName = "FernonF",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct1,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt17.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt17);
            }
            MapTypeDTO mt18 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Cliff,
                MapTypeName = "Cliff",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultAct1,
                ReturnMapTypeId = (long)RespawnType.ReturnAct1
            };
            if (list.All(s => s.MapTypeId != mt18.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt18);
            }
            MapTypeDTO mt19 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.LandOfTheDead,
                MapTypeName = "LandOfTheDead",
                PotionDelay = 300
            };
            if (list.All(s => s.MapTypeId != mt19.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt19);
            }
            MapTypeDTO mt20 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Act32,
                MapTypeName = "Act 3.2",
                PotionDelay = 300
            };
            if (list.All(s => s.MapTypeId != mt20.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt20);
            }
            MapTypeDTO mt21 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.CleftOfDarkness,
                MapTypeName = "Cleft of Darkness",
                PotionDelay = 300
            };
            if (list.All(s => s.MapTypeId != mt21.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt21);
            }
            MapTypeDTO mt23 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.CitadelAngel,
                MapTypeName = "AngelCitadel",
                PotionDelay = 300
            };
            if (list.All(s => s.MapTypeId != mt23.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt23);
            }
            MapTypeDTO mt24 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.CitadelDemon,
                MapTypeName = "DemonCitadel",
                PotionDelay = 300
            };
            if (list.All(s => s.MapTypeId != mt24.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt24);
            }
            MapTypeDTO mt25 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Oasis,
                MapTypeName = "Oasis",
                PotionDelay = 300,
                RespawnMapTypeId = (long)RespawnType.DefaultOasis,
                ReturnMapTypeId = (long)RespawnType.DefaultOasis
            };
            if (list.All(s => s.MapTypeId != mt25.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt25);
            }
            MapTypeDTO mt26 = new MapTypeDTO
            {
                MapTypeId = (short)MapTypeEnum.Act42,
                MapTypeName = "Act42",
                PotionDelay = 5000,
            };
            if (list.All(s => s.MapTypeId != mt26.MapTypeId))
            {
                DAOFactory.MapTypeDAO.InsertOrUpdate(ref mt26);
            }
            Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MAPTYPES_PARSED));
        }
    }
}