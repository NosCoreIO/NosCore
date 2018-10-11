using System.Collections.Generic;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Parsers
{
    internal class RespawnMapTypeParser
    {
        internal void InsertRespawnMapType()
        {
            var respawnmaptypemaps = new List<RespawnMapTypeDTO>
            {
                new RespawnMapTypeDTO
                {
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    DefaultMapId = 1,
                    DefaultX = 80,
                    DefaultY = 116,
                    Name = "Default"
                },
                new RespawnMapTypeDTO
                {
                    RespawnMapTypeId = (long) RespawnType.ReturnAct1,
                    DefaultMapId = 0,
                    DefaultX = 0,
                    DefaultY = 0,
                    Name = "Return"
                },
                new RespawnMapTypeDTO
                {
                    RespawnMapTypeId = (long) RespawnType.DefaultAct5,
                    DefaultMapId = 170,
                    DefaultX = 86,
                    DefaultY = 48,
                    Name = "DefaultAct5"
                },
                new RespawnMapTypeDTO
                {
                    RespawnMapTypeId = (long) RespawnType.ReturnAct5,
                    DefaultMapId = 0,
                    DefaultX = 0,
                    DefaultY = 0,
                    Name = "ReturnAct5"
                },
                new RespawnMapTypeDTO
                {
                    RespawnMapTypeId = (long) RespawnType.DefaultAct6,
                    DefaultMapId = 228,
                    DefaultX = 72,
                    DefaultY = 102,
                    Name = "DefaultAct6"
                },
                new RespawnMapTypeDTO
                {
                    RespawnMapTypeId = (long) RespawnType.DefaultAct62,
                    DefaultMapId = 228,
                    DefaultX = 72,
                    DefaultY = 102,
                    Name = "DefaultAct62"
                },
                new RespawnMapTypeDTO
                {
                    RespawnMapTypeId = (long) RespawnType.DefaultOasis,
                    DefaultMapId = 261,
                    DefaultX = 66,
                    DefaultY = 70,
                    Name = "DefaultOasis"
                }
            };
            IEnumerable<RespawnMapTypeDTO> respawnMapTypeDtos = respawnmaptypemaps;
            DAOFactory.RespawnMapTypeDAO.InsertOrUpdate(respawnMapTypeDtos);
            Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.RESPAWNTYPE_PARSED));
        }
    }
}