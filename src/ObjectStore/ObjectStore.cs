using ObjectStore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
#if  NETCOREAPP1_0
using System.Reflection;
#endif

[assembly: InternalsVisibleTo("ObjectStore.SqlClient")]
[assembly: InternalsVisibleTo("ObjectStore.Sqlite")]

namespace ObjectStore
{
    public class ObjectStore : IObjectProvider
    {
        #region Konstruktoren
        public ObjectStore()
        {
            _objectProviders = new Dictionary<Type, IObjectProvider>();
            _objectOwnerlist = new Dictionary<object, IObjectProvider>();
        }
        #endregion

        #region Membervariablen
        Dictionary<Type, IObjectProvider> _objectProviders;
        Dictionary<object, IObjectProvider> _objectOwnerlist;
        #endregion

        #region Funktionen
        #region Öffentlich
        public IObjectProvider RegisterObjectProvider(Type type, IObjectProvider objectProvider)
        {
            IObjectProvider returnValue = null;
            if (_objectProviders.ContainsKey(type))
            {
                returnValue = _objectProviders[type];
            }

            _objectProviders[type] = objectProvider;

            return returnValue;
        }

        public IObjectProvider RegisterObjectProvider(IObjectProvider objectProvider)
        {
            return RegisterObjectProvider(typeof(object), objectProvider);
        }
        #endregion

        #region IObjectProvider Members
        public IQueryable<T> GetQueryable<T>() where T : class
        {
            List<IObjectProvider> providers = GetProviderForType(typeof(T));
            if (providers.Count == 1)
            {
                return providers[0].GetQueryable<T>();
            }
            if (providers.Count > 1)
            {
                IQueryable<T> queryable = null;
                foreach (IObjectProvider provider in providers)
                {
                    if (queryable == null)
                    {
                        queryable = provider.GetQueryable<T>();
                    }
                    else
                    {
                        queryable.Union(provider.GetQueryable<T>());
                    }
                }
                return queryable;
            }
            return new List<T>().AsQueryable();
        }

        public T CreateObject<T>() where T : class
        {
            foreach (IObjectProvider provider in GetProviderForType(typeof(T)))
            {
                T obj = provider.CreateObject<T>();
                if (obj != null && !obj.Equals(default(T)))
                {
                    _objectOwnerlist.Add(obj, provider);
                    return obj;
                }
            }
            return default(T);
        }

        bool IObjectProvider.SupportsType(Type type)
        {
            return GetProviderForType(type).Count > 0;
        }
        #endregion

        #region Privat
        private List<IObjectProvider> GetProviderForType(Type type)
        {
            List<IObjectProvider> returnValue = new List<IObjectProvider>();

            foreach (KeyValuePair<Type, IObjectProvider> provider in _objectProviders)
            {
                if ((provider.Key.IsAssignableFrom(type) ||
                    type.IsAssignableFrom(provider.Key)) &&
                    provider.Value.SupportsType(type))
                {
                    returnValue.Add(provider.Value);
                }
            }
            return returnValue;
        }

        private IEnumerable<T> SetObjectOwner<T>(IEnumerable<T> list, IObjectProvider provider)
        {
            foreach (T item in list)
            {
                _objectOwnerlist[item] = provider;
            }
            return list;
        }
#endregion
        #endregion
    }
}
