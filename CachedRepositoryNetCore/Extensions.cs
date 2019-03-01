using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace CachedRepository.NetCore
{
    public static class Extensions
    {
        public static IServiceCollection AddAllCachedRepositoriesAsServices(this IServiceCollection services, Assembly repoContainerAssembly, 
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.AddLazyCache();
            var assembly = repoContainerAssembly;

            //Register all CachedRepos
            assembly.GetTypesAssignableFrom(typeof(CachedRepo<>)).ForEach((t)=>
            {
                services.Add(new ServiceDescriptor(t,t, lifetime));
            });

            //Register all CachedDictionaries
            assembly.GetTypesAssignableFrom(typeof(CachedDictionary<>)).ForEach((t)=>
            {
                services.Add(new ServiceDescriptor(t,t, lifetime));
            });

            //Register all CachedDictionaries
            assembly.GetTypesAssignableFrom(typeof(CachedObject<>)).ForEach((t)=>
            {
                services.Add(new ServiceDescriptor(t,t, lifetime));
            });
            return services;
        }

        private static List<Type> GetTypesAssignableFrom<T>(this Assembly assembly)
        {
            return assembly.GetTypesAssignableFrom(typeof(T));
        }

        private static List<Type> GetTypesAssignableFrom(this Assembly assembly, Type compareType)
        {
            List<Type> ret = new List<Type>();
            var types = assembly.DefinedTypes.ToArray();
            for (var i = 0; i<types.Length;i++)
            {
                var type = types[i];
                if (compareType == type) 
                    continue;
                if (compareType.IsAssignableFrom(type) || IsAssignableToGenericType(compareType, type))
                {
                    ret.Add(type);
                }
            }
            return ret;
        }

        private static bool IsAssignableToGenericType(Type givenType, Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            if (givenType.IsGenericType)
            {
                if (givenType.GetGenericTypeDefinition() == genericType)
                    return true;
                if (genericType?.BaseType != null && genericType.BaseType.IsGenericType && genericType.BaseType?.GetGenericTypeDefinition() == givenType)
                    return true;
            }

            Type baseType = givenType.BaseType;
            if (baseType == null) return false;

            return IsAssignableToGenericType(baseType, genericType);
        }


    }
}
