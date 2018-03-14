using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Collections;
using Microsoft.EntityFrameworkCore;
using NosCore.Domain;
using NosCore.Core.Logger;

namespace NosCore.Database
{
    public class GenericDAO<TEntity, TDTO> : IGenericDAO<TEntity, TDTO> where TEntity : class
    {
        protected readonly IDictionary<Type, Type> Mappings = new Dictionary<Type, Type>();
        protected IMapper Mapper;

        public PropertyInfo PrimaryKey { get; set; }

        public virtual void InitializeMapper()
        {
            MapperConfiguration config = new MapperConfiguration(cfg =>
            {
                foreach (KeyValuePair<Type, Type> entry in Mappings)
                {
                    // GameObject -> Entity
                    cfg.CreateMap(typeof(TDTO), entry.Value);

                    // Entity -> GameObject
                    cfg.CreateMap(entry.Value, typeof(TDTO))
                        .AfterMap((src, dest) => ((IDatabaseObject)dest).Initialize()).As(entry.Key);
                }
            });

            Mapper = config.CreateMapper();
        }

        public virtual IMappingBaseDAO RegisterMapping(Type gameObjectType)
        {
            try
            {
                Type targetType = typeof(TEntity);
                Mappings.Add(gameObjectType, targetType);

                foreach (PropertyInfo pi in gameObjectType.GetProperties())
                {
                    object[] attrs = pi.GetCustomAttributes(typeof(KeyAttribute), false);
                    if (attrs.Length != 1)
                    {
                        continue;
                    }
                    PrimaryKey = pi;
                    break;
                }
                if (PrimaryKey != null)
                {
                    return this;
                }
                throw new KeyNotFoundException();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
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
                    return Mapper.Map<TDTO>(ent);
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
                    TEntity entity = Mapper.Map<TEntity>(dto);
                    DbSet<TEntity> dbset = context.Set<TEntity>();

                    object value = PrimaryKey.GetValue(dto, null);
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
                        Mapper.Map(entity, entityfound);

                        context.Entry(entityfound).CurrentValues.SetValues(entity);
                        context.SaveChanges();

                    }
                    if (value == null || entityfound == null)
                    {
                        dbset.Add(entity);
                    }
                    context.SaveChanges();
                    dto = Mapper.Map<TDTO>(entity);

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
                        TEntity entity = Mapper.Map<TEntity>(dto);
                        object value = PrimaryKey.GetValue(dto, null);

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
                            Mapper.Map(entity, entityfound);

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
                    yield return Mapper.Map<TDTO>(t);
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
                    yield return Mapper.Map<TDTO>(t);
                }
            }
        }
    }
}
