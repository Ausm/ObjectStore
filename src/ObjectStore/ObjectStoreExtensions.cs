using ObjectStore.OrMapping;
using ObjectStore.Interfaces;
using Microsoft.Extensions.DependencyInjection;


namespace ObjectStore
{
    public static class ObjectStoreExtensions
    {
        public static void AddObjectStore(this IServiceCollection services, string connectionString)
        {
            RelationalObjectStore relationalObjectStore = new RelationalObjectStore(connectionString, true);
            ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(relationalObjectStore);
            services.Add(new ServiceDescriptor(typeof(IObjectProvider), relationalObjectStore));
        }
    }
}
