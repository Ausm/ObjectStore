using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ausm.ObjectStore.OrMapping
{
    /// <summary>
    /// IObjectProvider der Aufrufe an InheritensObjectProvider registrierter Typen weiterleitet.
    /// </summary>
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

        /// <summary>
        /// Registriert einen neuen InheritensObjectProvider vom angegebenen Typen
        /// </summary>
        /// <typeparam name="T">Type des zu registrierenden ObjectProviders</typeparam>
        /// <returns>Gibt immer this zurück um das Register mehrerer Typen in einer Anweisung schreiben zu können</returns>
        public RelationalObjectStore Register<T>() where T : class //new()
        {
            if (!_relationalObjectProvider.ContainsKey(typeof(T)))
            {
                IObjectProvider newProvider = new InheritensObjectProvider<T>(_connectionString);
                _relationalObjectProvider[typeof(T)] = newProvider;
            }
            return this;
        }

        #region IObjectProvider Members
        public IQueryable<T> GetQueryable<T>() where T : class
        {
            if (_autoregisterTypes && !_relationalObjectProvider.ContainsKey(typeof(T)))
            {
                Register<T>();
#if DEBUG
                throw new InvalidOperationException(string.Format("Objectstore for Type '{0}' is not Registered", typeof(T).FullName));
#else
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
                System.Diagnostics.Trace.TraceError("Type '{0}' is not registered in Objectstore.", typeof(T).FullName);
            }

            return _relationalObjectProvider[typeof(T)].CreateObject<T>();
        }

        public bool SupportsType(Type type)
        {
            if (_autoregisterTypes)
                return true;

            return _relationalObjectProvider[type].SupportsType(type);
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
