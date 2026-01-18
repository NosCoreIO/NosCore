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

using NosCore.Data.Dto;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.CharacterPersistenceService;

public class CharacterPersistenceService : ICharacterPersistenceService
{
    public CharacterDto SyncToDto(PlayerContext player)
    {
        var dto = player.CharacterData;
        var world = player.World;
        var entity = player.Entity;

        dto.Level = entity.GetCharacterLevel(world);
        dto.LevelXp = entity.GetLevelXp(world);
        dto.JobLevel = entity.GetJobLevel(world);
        dto.JobLevelXp = entity.GetJobLevelXp(world);
        dto.HeroXp = entity.GetHeroXp(world);
        dto.Gold = entity.GetGold(world);
        dto.Reput = entity.GetReput(world);
        dto.Dignity = entity.GetDignity(world);

        var combat = entity.GetCombat(world);
        if (combat != null)
        {
            dto.HeroLevel = combat.Value.HeroLevel;
        }

        var health = entity.GetHealth(world);
        if (health != null)
        {
            dto.Hp = health.Value.Hp;
        }

        var mana = entity.GetMana(world);
        if (mana != null)
        {
            dto.Mp = mana.Value.Mp;
        }

        var appearance = entity.GetAppearance(world);
        if (appearance != null)
        {
            dto.Class = appearance.Value.Class;
            dto.Gender = appearance.Value.Gender;
            dto.HairStyle = appearance.Value.HairStyle;
            dto.HairColor = appearance.Value.HairColor;
        }

        var sp = entity.GetSp(world);
        if (sp != null)
        {
            dto.SpPoint = sp.Value.SpPoint;
            dto.SpAdditionPoint = sp.Value.SpAdditionPoint;
            dto.Compliment = sp.Value.Compliment;
        }

        var pos = entity.GetPosition(world);
        if (pos != null)
        {
            dto.MapX = pos.Value.PositionX;
            dto.MapY = pos.Value.PositionY;
        }

        return dto;
    }
}
