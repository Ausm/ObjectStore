using ObjectStore.OrMapping;
using ObjectStore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ObjectStore.MappingOptions;
using System.Reflection;
using System;

namespace ObjectStore
{
    public static class ObjectStoreExtensions
    {
        public static void AddObjectStore(this IServiceCollection services, IDataBaseProvider databaseProvider, string connectionString, MappingOptionsSet mappingOptionsSet = null)
        {
            if (mappingOptionsSet == null)
                mappingOptionsSet = new MappingOptionsSet().AddDefaultRules();

            RelationalObjectStore relationalObjectStore = new RelationalObjectStore(connectionString, databaseProvider, mappingOptionsSet, true);
            ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(relationalObjectStore);
            services.Add(new ServiceDescriptor(typeof(IObjectProvider), relationalObjectStore));
        }

        public static MappingOptionsSet AddDefaultRules(this MappingOptionsSet mappingOptionsSet)
        {
            mappingOptionsSet.AddTypeRule(x => !x.GetTypeInfo().IsAbstract, o => { throw new NotSupportedException("InheritedPropertyMapping supports abstract classes and interfaces only."); });
            mappingOptionsSet.AddTypeRule(x => x.GetCustomAttribute<TableAttribute>() != null, o => {
                    TableAttribute attribute = o.Type.GetCustomAttribute<TableAttribute>();
                    o.TableName = attribute.TableName;
                    o.LoadBehavior = attribute.LoadBehavior;
                });

            return mappingOptionsSet;
        }


        internal static T GetCustomAttribute<T>(this Type type) where T : Attribute
        {
            return type.GetTypeInfo().GetCustomAttribute(typeof(TableAttribute), true) as T;
        }
    }
}
