using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using ObjectStore.OrMapping;
using Microsoft.Framework.DependencyInjection;
using ObjectStore.Interfaces;

#if !DNXCORE50
using System.Transactions;
#endif

namespace ObjectStore
{
    public static class Extensions
	{
        public static void AddObjectStore(this IServiceCollection services, string connectionString)
        {
            RelationalObjectStore relationalObjectStore = new RelationalObjectStore(connectionString, true);
            ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(relationalObjectStore);
            services.Add(new ServiceDescriptor(typeof(IObjectProvider), relationalObjectStore));
        }
    }
}
