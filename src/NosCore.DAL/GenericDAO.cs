using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Collections;
using Microsoft.EntityFrameworkCore;
using NosCore.Shared;
using NosCore.Database;
using NosCore.Data;
using NosCore.Shared.Logger;

namespace NosCore.DAL
{
    public class GenericDAO<TEntity, TDTO> where TEntity : class
    {
        private readonly IMapper _mapper;

        private readonly PropertyInfo _primaryKey;

        public GenericDAO(IMapper mapper)
        {
            try
            {
                Type targetType = typeof(TEntity);
                if (mapper == null)
                {
                    MapperConfiguration config = new MapperConfiguration(cfg => cfg.CreateMap(typeof(TDTO), targetType).ReverseMap());

                    _mapper = config.CreateMapper();
                }
                else
                {
                    _mapper = mapper;
                }

                foreach (PropertyInfo pi in typeof(TDTO).GetProperties())
                {
                    object[] attrs = pi.GetCustomAttributes(typeof(KeyAttribute), false);
                    if (attrs.Length != 1)
                    {
                        continue;
                    }
                    _primaryKey = pi;
                    break;
                }
                if (_primaryKey != null)
                {
                    return;
                }
                throw new KeyNotFoundException();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public SaveResult Delete(object dtokey)
        {
            using (NosCoreContext context = DataAccessHelper.Instance.CreateContext())
            {
                DbSet<TEntity> dbset = context.Set<TEntity>();

                if (dtokey is IEnumerable)
                {
                    foreach (object key in dtokey as IEnumerable)
                    {
                        TEntity entityfound = dbset.Find(key);

                        if (entityfound != null)
                        {
                            dbset.Remove(entityfound);
                            context.SaveChanges();
                        }
                    }
                }
                else
                {
                    TEntity entityfound = dbset.Find(dtokey);

                    if (entityfound != null)
                    {
                        dbset.Remove(entityfound);
                    }
                }
                context.SaveChanges();

                return SaveResult.Saved;
            }
        }

        public TDTO FirstOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                using (NosCoreContext context = DataAccessHelper.Instance.CreateContext())
                {
                    DbSet<TEntity> dbset = context.Set<TEntity>();
                    IEnumerable<TEntity> entities = Enumerable.Empty<TEntity>();
                    TEntity ent = dbset.FirstOrDefault(predicate);
                    return _mapper.Map<TDTO>(ent);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return default(TDTO);
            }
        }

        public SaveResult InsertOrUpdate(ref TDTO dto)
        {
            try
            {
                using (NosCoreContext context = DataAccessHelper.Instance.CreateContext())
                {
                    TEntity entity = _mapper.Map<TEntity>(dto);
                    DbSet<TEntity> dbset = context.Set<TEntity>();

                    object value = _primaryKey.GetValue(dto, null);
                    TEntity entityfound = null;
                    if (value is object[])
                    {
                        entityfound = dbset.Find((object[])value);
                    }
                    else
                    {
                        entityfound = dbset.Find(value);
                    }
                    if (entityfound != null)
                    {
                        _mapper.Map(entity, entityfound);

                        context.Entry(entityfound).CurrentValues.SetValues(entity);
                        context.SaveChanges();
                    }
                    if (value == null || entityfound == null)
                    {
                        dbset.Add(entity);
                    }
                    context.SaveChanges();
                    dto = _mapper.Map<TDTO>(entity);

                    return SaveResult.Saved;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(IEnumerable<TDTO> dtos)
        {
            try
            {
                using (NosCoreContext context = DataAccessHelper.Instance.CreateContext())
                {
                    context.ChangeTracker.AutoDetectChangesEnabled = false;

                    DbSet<TEntity> dbset = context.Set<TEntity>();
                    List<TEntity> entitytoadd = new List<TEntity>();
                    foreach (TDTO dto in dtos)
                    {
                        TEntity entity = _mapper.Map<TEntity>(dto);
                        object value = _primaryKey.GetValue(dto, null);

                        TEntity entityfound = null;
                        if (value is object[])
                        {
                            entityfound = dbset.Find((object[])value);
                        }
                        else
                        {
                            entityfound = dbset.Find(value);
                        }

                        if (entityfound != null)
                        {
                            _mapper.Map(entity, entityfound);

                            context.Entry(entityfound).CurrentValues.SetValues(entity);
                        }

                        if (value == null || entityfound == null)
                        {
                            //add in a temp list in order to avoid find(default(PK)) to find this element before savechanges
                            entitytoadd.Add(entity);
                        }
                    }

                    dbset.AddRange(entitytoadd);
                    context.ChangeTracker.AutoDetectChangesEnabled = true;
                    context.SaveChanges();
                    return SaveResult.Saved;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<TDTO> LoadAll()
        {
            using (NosCoreContext context = DataAccessHelper.Instance.CreateContext())
            {
                DbSet<TEntity> dbset = context.Set<TEntity>();
                foreach (TEntity t in dbset)
                {
                    yield return _mapper.Map<TDTO>(t);
                }
            }
        }

        public IEnumerable<TDTO> Where(Expression<Func<TEntity, bool>> predicate)
        {
            using (NosCoreContext context = DataAccessHelper.Instance.CreateContext())
            {
                DbSet<TEntity> dbset = context.Set<TEntity>();
                IEnumerable<TEntity> entities = Enumerable.Empty<TEntity>();
                try
                {
                    entities = dbset.Where(predicate).ToList();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
                foreach (TEntity t in entities)
                {
                    yield return _mapper.Map<TDTO>(t);
                }
            }
        }
    }
}
