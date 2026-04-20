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
using Microsoft.Extensions.DependencyInjection;
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

        if (_onDtoTypeRegistered != null)
        {
            foreach (var t in typeof(IStaticDto).Assembly.GetTypes()
                .Where(p => typeof(IDto).IsAssignableFrom(p) && p.IsClass))
            {
                _onDtoTypeRegistered(builder, t);
            }
        }

        var registerMethod = typeof(PersistenceModule).GetMethod(nameof(RegisterDatabaseObject), BindingFlags.Public | BindingFlags.Static)!;
        foreach (var mapping in DiscoverDaoMappings())
        {
            registerMethod.MakeGenericMethod(mapping.DtoType, mapping.DbType, mapping.PkType)
                .Invoke(null, new[] { builder, (object)mapping.IsStatic });
        }

        builder.RegisterType<Dao<ItemInstance, IItemInstanceDto?, Guid>>()
            .As<IDao<IItemInstanceDto?, Guid>>().SingleInstance();
    }

    // Mirror the DAO + DbContext registrations into IServiceCollection so frameworks that
    // inspect IServiceCollection at startup (Wolverine code-gen, ASP.NET Core's analyzer,
    // health-check scopes, etc.) can plan handler construction. The runtime IServiceProvider
    // is still backed by Autofac via AutofacServiceProviderFactory, so resolution semantics
    // are unchanged — this is a visibility shim, not a second source of truth.
    //
    // Note: we deliberately do NOT mirror the open IDao<IDto> facet here. PersistenceModule.Load
    // registers each Dao<TDb,TDto,TPk> as both IDao<IDto> and IDao<TDto,TPk> in Autofac. If we
    // mirror the IDao<IDto> facet too, Populate(services) reflows it into Autofac as a SECOND
    // registration, and Resolve<IEnumerable<IDao<IDto>>>().ToDictionary(by DTO type) blows up
    // with "An item with the same key has already been added" when the i18n dictionary is
    // built. Wolverine handlers want typed IDao<TDto,TPk>, not the IDto aggregation, so the
    // typed registration alone is sufficient for code-gen visibility.
    public static void MirrorTo(IServiceCollection services)
    {
        services.AddTransient<DbContext, NosCoreContext>();

        foreach (var mapping in DiscoverDaoMappings())
        {
            var daoType = typeof(Dao<,,>).MakeGenericType(mapping.DbType, mapping.DtoType, mapping.PkType);
            var idaoType = typeof(IDao<,>).MakeGenericType(mapping.DtoType, mapping.PkType);
            services.AddSingleton(idaoType, daoType);
            if (mapping.IsStatic)
            {
                var listType = typeof(List<>).MakeGenericType(mapping.DtoType);
                // The runtime IServiceProvider is Autofac-backed and has the populated list;
                // forwarding here lets Wolverine's planner see the type without duplicating
                // the load-all + i18n-injection logic.
                services.AddSingleton(listType, sp => sp.GetRequiredService(listType));
            }
        }

        services.AddSingleton<IDao<IItemInstanceDto?, Guid>, Dao<ItemInstance, IItemInstanceDto?, Guid>>();
    }

    public static IEnumerable<DaoMapping> DiscoverDaoMappings()
    {
        var assemblyDto = typeof(IStaticDto).Assembly.GetTypes();
        var assemblyDb = typeof(Account).Assembly.GetTypes();
        var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(Guid.NewGuid().ToString());

        foreach (var dto in assemblyDto.Where(p =>
            typeof(IDto).IsAssignableFrom(p) &&
            (!p.Name.Contains("InstanceDto") || p.Name.Contains("Inventory")) && p.IsClass))
        {
            var db = assemblyDb.FirstOrDefault(tgo =>
                string.Compare(dto.Name, $"{tgo.Name}Dto", StringComparison.OrdinalIgnoreCase) == 0);
            if (db == null)
            {
                continue;
            }
            var pk = db.GetProperties()
                .FirstOrDefault(s => new NosCoreContext(optionsBuilder.Options).Model.FindEntityType(db)?
                    .FindPrimaryKey()?.Properties.Select(x => x.Name)
                    .Contains(s.Name) ?? false);
            if (pk == null)
            {
                continue;
            }
            yield return new DaoMapping(dto, db, pk.PropertyType, typeof(IStaticDto).IsAssignableFrom(dto));
        }
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

    public sealed record DaoMapping(Type DtoType, Type DbType, Type PkType, bool IsStatic);
}
