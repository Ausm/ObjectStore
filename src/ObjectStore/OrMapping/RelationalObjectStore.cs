using ObjectStore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ObjectStore.OrMapping
{
    public class RelationalObjectStore : IObjectProvider, IObjectRegistration
    {
        Dictionary<Type, IObjectProvider> _relationalObjectProvider;
        string _connectionString;
        bool _autoregisterTypes;

        public RelationalObjectStore(string connectionString, bool autoregister)
        {
            _relationalObjectProvider = new Dictionary<Type, IObjectProvider>();
            _connectionString = connectionString;
            _autoregisterTypes = autoregister;
        }

        public RelationalObjectStore(string connectionString)
            : this(connectionString, false)
        {
        }

        public RelationalObjectStore Register<T>() where T : class //new()
        {
            if (!_relationalObjectProvider.ContainsKey(typeof(T)))
            {
                IObjectProvider newProvider = new InheritensObjectProvider<T>(_connectionString);
                _relationalObjectProvider[typeof(T)] = newProvider;

                System.Reflection.MethodInfo registerMethod = null;
                List<Type> subTypes = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                                            .Where(x => x.GetCustomAttributes(typeof(ForeignObjectMappingAttribute), true).Any())
                                            .Select(x => x.PropertyType).Where(x => !_relationalObjectProvider.ContainsKey(x))
                                            .Distinct().ToList();
                subTypes.AddRange(
                    typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                                            .Where(x => x.GetCustomAttributes(typeof(ReferenceListMappingAttribute), true).Any())
                                            .Select(x => x.PropertyType.GetGenericArguments().First()).Where(x => !_relationalObjectProvider.ContainsKey(x))
                                            .Distinct());

                foreach (Type subType in subTypes.Where(x => x != typeof(T)).Distinct())
                {
                    if(registerMethod == null)
                        registerMethod = typeof(RelationalObjectStore).GetMethod("Register");

                    System.Linq.Expressions.Expression.Lambda<Action>(
                        System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.Constant(this), registerMethod.MakeGenericMethod(subType)))
                            .Compile()();
                }
            }
            return this;
        }

        #region IObjectProvider Members
        public IQueryable<T> GetQueryable<T>() where T : class
        {
            if (_autoregisterTypes && !_relationalObjectProvider.ContainsKey(typeof(T)))
            {
                Register<T>();
#if DEBUG && !DNXCORE50
                System.Diagnostics.Trace.TraceError("Type '{0}' is not registered in Objectstore.", typeof(T).FullName);
#endif
            }

            return _relationalObjectProvider[typeof(T)].GetQueryable<T>();
        }

        public T CreateObject<T>() where T : class
        {
            if (_autoregisterTypes && !_relationalObjectProvider.ContainsKey(typeof(T)))
            {
                Register<T>();
#if DEBUG && !DNXCORE50
                System.Diagnostics.Trace.TraceError("Type '{0}' is not registered in Objectstore.", typeof(T).FullName);
#endif
            }

            return _relationalObjectProvider[typeof(T)].CreateObject<T>();
        }

        public bool SupportsType(Type type)
        {
            if (_autoregisterTypes)
                return true;

            return _relationalObjectProvider.ContainsKey(type) && _relationalObjectProvider[type].SupportsType(type);
        }
#endregion

#region IObjectRegistration Members

        IObjectRegistration IObjectRegistration.Register<T>()
        {
            return this.Register<T>();
        }

#endregion
    }

    public interface IObjectRegistration
    {
        IObjectRegistration Register<T>() where T : class;
    }
}
