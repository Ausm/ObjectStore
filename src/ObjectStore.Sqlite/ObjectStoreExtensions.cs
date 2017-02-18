using ObjectStore.OrMapping;
using ObjectStore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ObjectStore.MappingOptions;

namespace ObjectStore.Sqlite
{
    public static class ObjectStoreSqliteExtensions
    {
        public static void AddObjectStoreWithSqlite(this IServiceCollection services, string connectionString, MappingOptionsSet mappingOptionsSet = null)
        {
            if (mappingOptionsSet == null)
                mappingOptionsSet = new MappingOptionsSet().AddDefaultRules();

            RelationalObjectStore relationalObjectStore = new RelationalObjectStore(connectionString, DataBaseProvider.Instance, mappingOptionsSet, true);
            ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(relationalObjectStore);
            services.Add(new ServiceDescriptor(typeof(IObjectProvider), relationalObjectStore));
        }
    }
}
