//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using FastMember;
using Microsoft.EntityFrameworkCore;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data;
using NosCore.Data.DataAttributes;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Database.Entities;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Database.Hosting;

public sealed class PersistenceModule : Autofac.Module
{
    private readonly Action<ContainerBuilder, Type>? _onDtoTypeRegistered;

    public PersistenceModule(Action<ContainerBuilder, Type>? onDtoTypeRegistered = null)
    {
        _onDtoTypeRegistered = onDtoTypeRegistered;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<NosCoreContext>().As<DbContext>();

        builder.Register(c => c.Resolve<IEnumerable<IDao<IDto>>>().OfType<IDao<II18NDto>>().ToDictionary(
                x => x.GetType().GetGenericArguments()[1], y => y.LoadAll().GroupBy(x => x.Key ?? "")
                    .ToDictionary(x => x.Key,
                        x => x.ToList().ToDictionary(o => o.RegionType, o => o))))
            .AsImplementedInterfaces()
            .SingleInstance()
            .AutoActivate();

        var assemblyDto = typeof(IStaticDto).Assembly.GetTypes();
        var assemblyDb = typeof(Account).Assembly.GetTypes();

        if (_onDtoTypeRegistered != null)
        {
            foreach (var t in assemblyDto.Where(p => typeof(IDto).IsAssignableFrom(p) && p.IsClass))
            {
                _onDtoTypeRegistered(builder, t);
            }
        }

        var registerMethod = typeof(PersistenceModule).GetMethod(nameof(RegisterDatabaseObject), BindingFlags.Public | BindingFlags.Static)!;
        foreach (var t in assemblyDto.Where(p =>
            typeof(IDto).IsAssignableFrom(p) &&
            (!p.Name.Contains("InstanceDto") || p.Name.Contains("Inventory")) && p.IsClass))
        {
            var type = assemblyDb.First(tgo =>
                string.Compare(t.Name, $"{tgo.Name}Dto", StringComparison.OrdinalIgnoreCase) == 0);
            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(Guid.NewGuid().ToString());
            var typepk = type.GetProperties()
                .Where(s => new NosCoreContext(optionsBuilder.Options).Model.FindEntityType(type)?
                    .FindPrimaryKey()?.Properties.Select(x => x.Name)
                    .Contains(s.Name) ?? false)
                .ToArray()[0];
            registerMethod.MakeGenericMethod(t, type, typepk.PropertyType)
                .Invoke(null, new[] { builder, (object)typeof(IStaticDto).IsAssignableFrom(t) });
        }

        builder.RegisterType<Dao<ItemInstance, IItemInstanceDto?, Guid>>()
            .As<IDao<IItemInstanceDto?, Guid>>().SingleInstance();
    }

    public static void RegisterDatabaseObject<TDto, TDb, TPk>(ContainerBuilder builder, bool isStatic)
        where TDb : class
        where TPk : struct
    {
        builder.RegisterType<Dao<TDb, TDto, TPk>>().As<IDao<IDto>>().As<IDao<TDto, TPk>>().SingleInstance();
        if (!isStatic)
        {
            return;
        }

        var staticMetaDataAttribute = typeof(TDb).GetCustomAttribute<StaticMetaDataAttribute>();
        builder.Register(c =>
        {
            var dic = c.Resolve<IDictionary<Type, Dictionary<string, Dictionary<RegionType, II18NDto>>>>();
            var items = c.Resolve<IDao<TDto, TPk>>().LoadAll().ToList();
            var props = StaticDtoExtension.GetI18NProperties(typeof(TDto));
            if (props.Count > 0)
            {
                var regions = Enum.GetValues(typeof(RegionType));
                var accessors = TypeAccessor.Create(typeof(TDto));
                Parallel.ForEach(items, s => ((IStaticDto)s!).InjectI18N(props, dic, regions, accessors));
            }

            if (items.Count != 0 || staticMetaDataAttribute == null ||
                staticMetaDataAttribute.EmptyMessage == LogLanguageKey.UNKNOWN)
            {
                if (staticMetaDataAttribute != null &&
                    staticMetaDataAttribute.LoadedMessage != LogLanguageKey.UNKNOWN)
                {
                    c.Resolve<ILogger>().Information(
                        c.Resolve<ILogLanguageLocalizer<LogLanguageKey>>()[staticMetaDataAttribute.LoadedMessage],
                        items.Count);
                }
            }
            else
            {
                c.Resolve<ILogger>().Error(
                    c.Resolve<ILogLanguageLocalizer<LogLanguageKey>>()[staticMetaDataAttribute.EmptyMessage]);
            }

            return items;
        })
            .As<List<TDto>>()
            .SingleInstance()
            .AutoActivate();
    }
}
