namespace NosCore.GameObject.DependancyInjection
{
    public interface IDependencyResolver
    {
        T Resolve<T>();
    }
}
