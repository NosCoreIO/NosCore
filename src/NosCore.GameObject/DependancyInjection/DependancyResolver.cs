using System;

namespace NosCore.GameObject.DependancyInjection
{
    public class FuncDependencyResolver : IDependencyResolver
    {
        private readonly Func<Type, object> _resolver;

        public FuncDependencyResolver(Func<Type, object> resolver)
        {
            _resolver = resolver;
        }

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            return _resolver(type);
        }
    }
}
