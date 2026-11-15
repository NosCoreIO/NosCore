//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.TalentArenaService
{
    // The Arena of Masters record: the three characters with the most master points in each class.
    // Characters online right now count with their unsaved points.
    public class MasterArenaRankingService(IDao<CharacterDto, long> characterDao, ISessionRegistry sessionRegistry,
            IOptions<WorldConfiguration> worldConfiguration, IClock clock)
        : IMasterArenaRankingService, ISingletonService
    {
        public const int Places = 3;

        private static readonly CharacterClassType[] Classes =
            [CharacterClassType.Swordsman, CharacterClassType.Archer, CharacterClassType.Mage, CharacterClassType.MartialArtist];

        public TARankPacket GetRanking()
        {
            var serverId = worldConfiguration.Value.ServerId;
            var online = sessionRegistry.GetCharacters().Select(c => c.CharacterDto)
                .ToDictionary(c => c.CharacterId);
            var saved = characterDao.Where(c => c.ServerId == serverId && c.State == CharacterState.Active && c.MasterPoints > 0)?
                .ToList() ?? [];
            var characters = saved.Where(c => !online.ContainsKey(c.CharacterId)).Concat(online.Values
                .Where(c => c.ServerId == serverId && c.State == CharacterState.Active));
            var now = clock.GetCurrentInstant().InZone(DateTimeZoneProviders.Tzdb.GetSystemDefault()).LocalDateTime;
            return Build(characters, now);
        }

        // Twelve places, three per class from Swordsman to Martial Artist, then the four last
        // winners of the month, which nothing records yet.
        public static TARankPacket Build(IEnumerable<CharacterDto> characters, LocalDateTime now)
        {
            var ranked = characters.Where(c => c.MasterPoints > 0).ToList();
            var places = Classes.SelectMany(type => ranked.Where(c => c.Class == type)
                    .OrderByDescending(c => c.MasterPoints)
                    .ThenBy(c => c.Name, StringComparer.Ordinal)
                    .Select(Place)
                    .Concat(Enumerable.Repeat(new TARankSubPacket(), Places))
                    .Take(Places))
                .Concat(Classes.Select(_ => new TARankSubPacket()))
                .ToList<TARankSubPacket?>();
            return new TARankPacket
            {
                CharacterList = places,
                Date = now.ToString("yyMMddHHmm", null),
            };
        }

        private static TARankSubPacket Place(CharacterDto character) => new()
        {
            Score = (short)Math.Min(character.MasterPoints, short.MaxValue),
            Level = character.Level,
            HeroLevel = character.HeroLevel,
            Name = character.Name,
            CharacterClass = character.Class,
            Gender = character.Gender,
            Morph = 0,
        };
    }
}
