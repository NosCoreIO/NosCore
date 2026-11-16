//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Messaging.Events;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Messaging
{
    public interface IGameEventDispatcher
    {
        Task RaiseAsync<TEvent>(TEvent gameEvent) where TEvent : IGameEvent;
    }

    // Every class of this assembly or the event's with a public Handle or HandleAsync taking one event handles it.
    // Each raise builds the handlers from the container, like a message handler, and runs them one
    // after another; one that throws is logged and the next still runs.
    public sealed class GameEventDispatcher(IServiceProvider services, ILogger<GameEventDispatcher> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage) : IGameEventDispatcher
    {
        public async Task RaiseAsync<TEvent>(TEvent gameEvent) where TEvent : IGameEvent
        {
            if (gameEvent is IPlayerEvent { PlayerHasLeft: true })
            {
                return;
            }

            foreach (var handler in Handlers<TEvent>.All)
            {
                try
                {
                    await handler.Handle(handler.Create(services, null), gameEvent);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, logLanguage[LogLanguageKey.GAME_EVENT_HANDLER_FAILED], handler.Type.Name, typeof(TEvent).Name);
                }
            }
        }

        public static IReadOnlyList<Type> HandlerTypesOf<TEvent>() where TEvent : IGameEvent =>
            Handlers<TEvent>.All.Select(h => h.Type).ToList();

        private sealed record Handler<TEvent>(Type Type, ObjectFactory Create, Func<object, TEvent, Task> Handle);

        private static class Handlers<TEvent>
        {
            public static readonly Handler<TEvent>[] All = new[] { typeof(GameEventDispatcher).Assembly, typeof(TEvent).Assembly }
                .Distinct()
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(IsHandleOf).Select(m => (Type: t, Method: m)))
                .OrderBy(h => h.Type.GetCustomAttribute<GameEventOrderAttribute>()?.Order ?? 0)
                .ThenBy(h => h.Type.FullName, StringComparer.Ordinal)
                .Select(h => new Handler<TEvent>(h.Type, ActivatorUtilities.CreateFactory(h.Type, Type.EmptyTypes), Compile(h.Type, h.Method)))
                .ToArray();

            private static bool IsHandleOf(MethodInfo method) =>
                method.Name is "Handle" or "HandleAsync"
                && method.GetParameters() is [{ ParameterType: var parameter }]
                && parameter == typeof(TEvent)
                && (method.ReturnType == typeof(Task) || method.ReturnType == typeof(void));

            private static Func<object, TEvent, Task> Compile(Type type, MethodInfo method)
            {
                var instance = Expression.Parameter(typeof(object));
                var gameEvent = Expression.Parameter(typeof(TEvent));
                Expression call = Expression.Call(Expression.Convert(instance, type), method, gameEvent);
                if (method.ReturnType == typeof(void))
                {
                    call = Expression.Block(call, Expression.Constant(Task.CompletedTask));
                }

                return Expression.Lambda<Func<object, TEvent, Task>>(call, instance, gameEvent).Compile();
            }
        }
    }
}
