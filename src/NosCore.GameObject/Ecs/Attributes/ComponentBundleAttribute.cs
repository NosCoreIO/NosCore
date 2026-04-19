//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.Ecs.Attributes
{
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class ComponentBundleAttribute : Attribute
    {
        public Type[] Components { get; }

        public ComponentBundleAttribute(params Type[] components)
        {
            Components = components;
        }
    }
}
