using ObjectStore.OrMapping;
using ObjectStore.Interfaces;
using Microsoft.Extensions.DependencyInjection;


namespace ObjectStore.SqlClient
{
    public static class ObjectStoreSqlClientExtensions
    {
        public static void AddObjectStoreWithSqlClient(this IServiceCollection services, string connectionString)
        {
            RelationalObjectStore relationalObjectStore = new RelationalObjectStore(connectionString, DataBaseProvider.Instance, true);
            ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(relationalObjectStore);
            services.Add(new ServiceDescriptor(typeof(IObjectProvider), relationalObjectStore));
        }
    }
}
