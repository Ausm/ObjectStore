using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Ausm.ObjectStore
{
    /// <summary>
    /// IObjectProvider der Aufrufe an registrierte IObjectProvider weiterleitet
    /// </summary>
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
        // hält alle registrierten IObjectProvider
        Dictionary<Type, IObjectProvider> _objectProviders;

        // Speichert alle objecte von einem IObjectProvider um alle Delete und DropChanges an den richtigen IObjectProvider weiterzuleiten.
        Dictionary<object, IObjectProvider> _objectOwnerlist;
        #endregion

        #region Funktionen
        #region Öffentlich
        /// <summary>
        /// Registriert einen ObjectProvider für einen Typen
        /// </summary>
        /// <param name="type">Type der objecte die über den Registrierten ObjectProvider abgerufen werden können</param>
        /// <param name="objectProvider">Der zu registrierende IObjectProvider</param>
        /// <returns>Gibt den IObjectProvider zurück der vorher auf diesen Type Registriert war, Null wenn noch keiner registiert war.</returns>
        public IObjectProvider RegisterObjectProvider(Type type, IObjectProvider objectProvider)
        {
            // Überprüfen ob bereits ein IObjectProvider auf diesem Type Registriert wurde
            IObjectProvider returnValue = null;
            if (_objectProviders.ContainsKey(type))
            {
                returnValue = _objectProviders[type];
            }

            // IObjectProvider setzen
            _objectProviders[type] = objectProvider;

            return returnValue;
        }


        /// <summary>
        /// Registriert einen ObjectProvider für Object, also für alle Typen
        /// </summary>
        /// <param name="objectProvider">Der zu registrierende IObjectProvider</param>
        /// <returns>Gibt den IObjectProvider zurück der vorher Registriert war.</returns>
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

        /// <summary>
        /// Instanziert ein Object vom Type T, welches beim nächsten Speichern geinserted wird.
        /// </summary>
        /// <returns>Instanziertes Object</returns>
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

        /// <summary>
        /// Gibt an ob Objecte vom angegebene Type abgerufen werden können.
        /// </summary>
        /// <param name="type">angefrager Type</param>
        /// <returns>true wenn Objecte dieses Types abgerufen werden können, andernfalls false</returns>
        bool IObjectProvider.SupportsType(Type type)
        {
            return GetProviderForType(type).Count > 0;
        }
        #endregion

        #region Privat
        /// <summary>
        /// Gibt alle verfügbaren IObjectProvider zurück die Objekte des übergebenen Typs zurückgeben.
        /// </summary>
        /// <param name="type">Type der Objekte die aus den IObjectProvidern abgerufen werden soll</param>
        /// <returns>Liste von IObjectProvidern</returns>
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

        /// <summary>
        /// Weist einer Liste von Objekten einem IObjectProvider zu um 
        /// Delete und Drop Changes eines Objects wieder nur auf den entsprechenden IObjectProvider anzuwenden.
        /// </summary>
        /// <param name="list">Liste von Objekten</param>
        /// <param name="provider">ObjectProvider</param>
        /// <returns>list-Parameter</returns>
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
