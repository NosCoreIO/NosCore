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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Mapster;
using Microsoft.EntityFrameworkCore;
using NosCore.Core;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Database.DAL;
using NosCore.Database.Entities;
using Serilog;

namespace NosCore.Database
{
    public class ItemInstanceDao : IDao<IItemInstanceDto?, Guid>
    {
        private readonly ILogger _logger;
        private readonly PropertyInfo[] _primaryKey;
        private readonly IDbContextBuilder _dbContextBuilder;

        public ItemInstanceDao(ILogger logger, IDbContextBuilder dbContextBuilder)
        {
            _logger = logger;
            _dbContextBuilder = dbContextBuilder;
            using var context = _dbContextBuilder.CreateContext();
            var key = typeof(IItemInstanceDto).GetProperties()
                .Where(s => s.Name == "Id").ToArray();
            _primaryKey = key.Any() ? key : throw new KeyNotFoundException();
        }

        public async Task<IItemInstanceDto?> TryDeleteAsync(Guid dtokey)
        {
            await using var context = _dbContextBuilder.CreateContext();
            var dbset = context.Set<ItemInstance>();
            object? value;
            try
            {
                value = _primaryKey.First()?.GetValue(dtokey, null);
            }
            catch
            {
                value = dtokey;
            }

            var entityfound = await dbset.FindAsync(value).ConfigureAwait(false);

            if (entityfound != null)
            {
                dbset.Remove(entityfound);
            }


            context.SaveChanges();
            return entityfound is BoxInstance ? entityfound.Adapt<BoxInstanceDto>() :
                entityfound is SpecialistInstance ? entityfound.Adapt<SpecialistInstanceDto>() :
                entityfound is WearableInstance ? entityfound.Adapt<WearableInstanceDto>() :
                entityfound is UsableInstance ? entityfound.Adapt<UsableInstanceDto>() :
                entityfound?.Adapt<ItemInstanceDto>();
        }

        public async Task<IEnumerable<IItemInstanceDto?>?> TryDeleteAsync(IEnumerable<Guid> dtokeys)
        {
            using var context = _dbContextBuilder.CreateContext();
            var dbset = context.Set<ItemInstance>();
            var temp = new List<IItemInstanceDto?>();
            foreach (var dto in dtokeys)
            {
                object? value = _primaryKey.First()?.GetValue(dto, null);

                ItemInstance? entityfound = value is object[] objects ? await dbset.FindAsync(objects).ConfigureAwait(false) : await dbset.FindAsync(value).ConfigureAwait(false);
                if (entityfound == null)
                {
                    continue;
                }

                dbset.Remove(entityfound);
                context.SaveChanges();
                temp.Add((entityfound is BoxInstance ? entityfound.Adapt<BoxInstanceDto>() :
                    entityfound is SpecialistInstance ? entityfound.Adapt<SpecialistInstanceDto>() :
                    entityfound is WearableInstance ? entityfound.Adapt<WearableInstanceDto>() :
                    entityfound is UsableInstance ? entityfound.Adapt<UsableInstanceDto>() :
                    entityfound.Adapt<ItemInstanceDto>()));
            }

            return temp;
        }

        public async Task<IItemInstanceDto?> FirstOrDefaultAsync(Expression<Func<IItemInstanceDto?, bool>> predicate)
        {
            try
            {
                if (predicate == null)
                {
                    return default!;
                }

                ItemInstance ent;
                await using var context = _dbContextBuilder.CreateContext();
                {
                    var dbset = context.Set<ItemInstance>();
                    ent = await dbset.FirstOrDefaultAsync(predicate!.ReplaceParameter<IItemInstanceDto, ItemInstance>()).ConfigureAwait(false);
                }

                return ent is BoxInstance ? ent.Adapt<BoxInstanceDto>() :
                    ent is SpecialistInstance ? ent.Adapt<SpecialistInstanceDto>() :
                    ent is WearableInstance ? ent.Adapt<WearableInstanceDto>() :
                    ent is UsableInstance ? ent.Adapt<UsableInstanceDto>() :
                    ent.Adapt<ItemInstanceDto>();
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
                return default!;
            }
        }

        public async Task<IItemInstanceDto?> TryInsertOrUpdateAsync(IItemInstanceDto? dto)
        {
            try
            {
                await using var context = _dbContextBuilder.CreateContext();

                var entity = dto!.GetType().Name == "BoxInstance" ? dto.Adapt<BoxInstance>()
                    : dto.GetType().Name == "SpecialistInstance" ? dto.Adapt<SpecialistInstance>()
                    : dto.GetType().Name == "WearableInstance" ? dto.Adapt<WearableInstance>()
                    : dto.GetType().Name == "UsableInstance" ? dto.Adapt<UsableInstance>()
                    : dto.Adapt<ItemInstance>();

                var dbset = context.Set<ItemInstance>();

                var value = _primaryKey.First()?.GetValue(dto, null);
                ItemInstance entityfound = value is object[] objects ? await dbset.FindAsync(objects).ConfigureAwait(false) : await dbset.FindAsync(value).ConfigureAwait(false);
                if (entityfound != null)
                {
                    context.Entry(entityfound).CurrentValues.SetValues(entity);
                    context.SaveChanges();
                }

                if ((value == null) || (entityfound == null))
                {
                    dbset.Add(entity);
                }

                context.SaveChanges();
                dto = entity is BoxInstance ? entity.Adapt<BoxInstanceDto>() :
                    entity is SpecialistInstance ? entity.Adapt<SpecialistInstanceDto>() :
                    entity is WearableInstance ? entity.Adapt<WearableInstanceDto>() :
                    entity is UsableInstance ? entity.Adapt<UsableInstanceDto>() :
                    entity.Adapt<ItemInstanceDto>();
                return dto;
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
                return (IItemInstanceDto?)null;
            }
        }

        public Task<bool> TryInsertOrUpdateAsync(IEnumerable<IItemInstanceDto?> dtos)
        {
            try
            {
                using var context = _dbContextBuilder.CreateContext();

                context.ChangeTracker.AutoDetectChangesEnabled = false;

                var dbset = context.Set<ItemInstance>();
                var entitytoadd = new List<ItemInstance>();
                var list = new List<Tuple<ItemInstance, Guid>>();

                foreach (var dto in dtos)
                {
                    list.Add(new Tuple<ItemInstance, Guid>(
                        dto!.GetType().Name == "BoxInstance" ? dto.Adapt<BoxInstance>()
                            : dto.GetType().Name == "SpecialistInstance" ? dto.Adapt<SpecialistInstance>()
                                : dto.GetType().Name == "WearableInstance" ? dto.Adapt<WearableInstance>()
                                    : dto.GetType().Name == "UsableInstance" ? dto.Adapt<UsableInstance>()
                                        : dto.Adapt<ItemInstance>(), (Guid)_primaryKey.First()!.GetValue(dto, null)!));
                }

                var ids = list.Select(s => s.Item2).ToArray();
                var dbkey = typeof(ItemInstance).GetProperty(_primaryKey!.First().Name);
                var entityfounds = dbset.FindAll(dbkey!, ids).ToList();

                foreach (var dto in list)
                {
                    var entity = dto.Item1;
                    var entityfound =
                        entityfounds.FirstOrDefault(s => (dynamic?)dbkey?.GetValue(s, null) == (dynamic)dto.Item2);
                    if (entityfound != null)
                    {
                        context.Entry(entityfound).CurrentValues.SetValues(entity);
                        continue;
                    }

                    entitytoadd.Add(entity);
                }

                dbset.AddRange(entitytoadd);

                context.ChangeTracker.AutoDetectChangesEnabled = true;
                context.SaveChanges();

                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
                return Task.FromResult(false);
            }
        }

        public IEnumerable<IItemInstanceDto> LoadAll()
        {
            using var context = _dbContextBuilder.CreateContext();
            foreach (var t in context.Set<ItemInstance>())
            {
                yield return t is BoxInstance ? t.Adapt<BoxInstanceDto>() :
                    t is SpecialistInstance ? t.Adapt<SpecialistInstanceDto>() :
                    t is WearableInstance ? t.Adapt<WearableInstanceDto>() :
                    t is UsableInstance ? t.Adapt<UsableInstanceDto>() :
                    t.Adapt<ItemInstanceDto>();
            }
        }

        public IEnumerable<IItemInstanceDto>? Where(Expression<Func<IItemInstanceDto?, bool>> predicate)
        {
            using var context = _dbContextBuilder.CreateContext();
            var dbset = context.Set<ItemInstance>();
            var entities = Enumerable.Empty<ItemInstance>();
            try
            {
                entities = dbset.Where(predicate!.ReplaceParameter<IItemInstanceDto, ItemInstance>());
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
            }

            foreach (var t in entities)
            {
                yield return t is BoxInstance ? t.Adapt<BoxInstanceDto>() :
                    t is SpecialistInstance ? t.Adapt<SpecialistInstanceDto>() :
                    t is WearableInstance ? t.Adapt<WearableInstanceDto>() :
                    t is UsableInstance ? t.Adapt<UsableInstanceDto>() :
                    t.Adapt<ItemInstanceDto>();
            }
        }
    }
}