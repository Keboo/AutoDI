using System;
using System.Linq;
using System.Reflection;
using StructureMap.Graph;
using StructureMap.Pipeline;

namespace StructureMap
{
    internal class AspNetConstructorSelector : IConstructorSelector
    {
        // ASP.NET expects registered services to be considered when selecting a ctor, SM doesn't by default.
        public ConstructorInfo Find(Type pluggedType, DependencyCollection dependencies, PluginGraph graph)
        {
            var typeInfo = pluggedType.GetTypeInfo();

            var constructors = typeInfo.DeclaredConstructors;

            var publicConstructors = constructors
                .Where(PublicConstructors)
                .Select(ctor => new Holder(ctor))
                .ToArray();

            var validConstructors = publicConstructors
                .Where(x => x.CanSatisfy(dependencies, graph))
                .ToArray();

            return validConstructors
                .OrderByDescending(x => x.Order)
                .Select(x => x.Constructor)
                .FirstOrDefault();
        }

        private static bool PublicConstructors(ConstructorInfo constructor)
        {
            // IsConstructor is false for static constructors.
            return constructor.IsConstructor && !constructor.IsPrivate;
        }

        private struct Holder
        {
            public Holder(ConstructorInfo constructor)
            {
                Constructor = constructor;
                Parameters = constructor.GetParameters();
            }

            public ConstructorInfo Constructor { get; }

            public int Order => Parameters.Length;

            private ParameterInfo[] Parameters { get; }

            public bool CanSatisfy(DependencyCollection dependencies, PluginGraph graph)
            {
                foreach (var parameter in Parameters)
                {
                    var type = parameter.ParameterType;

                    if (type.IsGenericEnumerable())
                    {
                        // Because graph.HasFamily returns false for IEnumerable<T>,
                        // we unwrap the generic argument and pass that instead.
                        type = type.GenericTypeArguments[0];
                    }

                    if (graph.HasFamily(type))
                    {
                        continue;
                    }

                    if (dependencies.Any(dep => dep.Type == type))
                    {
                        continue;
                    }

                    return false;
                }

                return true;
            }
        }
    }
}
