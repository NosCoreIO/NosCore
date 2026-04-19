using System;

namespace NosCore.GameObject.Ecs.Attributes;

[AttributeUsage(AttributeTargets.Struct)]
public class ComponentBundleAttribute : Attribute
{
    public Type[] Components { get; }

    public ComponentBundleAttribute(params Type[] components)
    {
        Components = components;
    }
}
