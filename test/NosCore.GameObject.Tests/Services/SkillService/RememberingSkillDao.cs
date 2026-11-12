//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;

namespace NosCore.GameObject.Tests.Services.SkillService
{
    // A store that remembers, which a Mock does not: these tests are about what ends up in the
    // rows, and a DAO that answers nothing to every query cannot show it.
    internal sealed class RememberingSkillDao : IDao<CharacterSkillDto, Guid>
    {
        public List<CharacterSkillDto> Rows { get; } = new();

        public Task<CharacterSkillDto> TryInsertOrUpdateAsync(CharacterSkillDto dto)
        {
            Rows.RemoveAll(r => r.Id == dto.Id);
            Rows.Add(dto);
            return Task.FromResult(dto);
        }

        public Task<bool> TryInsertOrUpdateAsync(IEnumerable<CharacterSkillDto> dtos)
        {
            var list = dtos.ToList();
            foreach (var dto in list)
            {
                Rows.RemoveAll(r => r.Id == dto.Id);
                Rows.Add(dto);
            }

            return Task.FromResult(list.Count > 0);
        }

        public Task<CharacterSkillDto?> FirstOrDefaultAsync(
            System.Linq.Expressions.Expression<Func<CharacterSkillDto, bool>> predicate) =>
            Task.FromResult<CharacterSkillDto?>(Rows.AsQueryable().FirstOrDefault(predicate));

        public IEnumerable<CharacterSkillDto>? Where(
            System.Linq.Expressions.Expression<Func<CharacterSkillDto, bool>> predicate) =>
            Rows.AsQueryable().Where(predicate).ToList();

        public Task<CharacterSkillDto?> TryDeleteAsync(Guid key)
        {
            var row = Rows.FirstOrDefault(r => r.Id == key);
            if (row != null)
            {
                Rows.Remove(row);
            }

            return Task.FromResult<CharacterSkillDto?>(row);
        }

        public Task<IEnumerable<CharacterSkillDto>?> TryDeleteAsync(IEnumerable<Guid> keys)
        {
            var removed = new List<CharacterSkillDto>();
            foreach (var key in keys)
            {
                var row = Rows.FirstOrDefault(r => r.Id == key);
                if (row == null)
                {
                    continue;
                }

                Rows.Remove(row);
                removed.Add(row);
            }

            return Task.FromResult<IEnumerable<CharacterSkillDto>?>(removed);
        }

        public IEnumerable<CharacterSkillDto> LoadAll() => Rows;
    }
}
