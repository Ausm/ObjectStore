using ObjectStore.OrMapping;
using ObjectStore.Interfaces;
using Microsoft.Extensions.DependencyInjection;


namespace ObjectStore.Sqlite
{
    public static class ObjectStoreSqliteExtensions
    {
        public static void AddObjectStore(this IServiceCollection services, string connectionString)
        {
            RelationalObjectStore relationalObjectStore = new RelationalObjectStore(connectionString, DataBaseProvider.Instance, true);
            ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(relationalObjectStore);
            services.Add(new ServiceDescriptor(typeof(IObjectProvider), relationalObjectStore));
        }
    }
}
