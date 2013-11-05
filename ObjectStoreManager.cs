using System;
using System.Collections.Generic;
using System.Text;

namespace Ausm.ObjectStore
{
    /// <summary>
    /// Singleton-Implementierung für die Default verwendete ObjectStore-Klasse
    /// </summary>
    public static class ObjectStoreManager
    {
        static ObjectStore _defaultObjectStore = null;
        static int _currentUniqe = 0;

        public static ObjectStore DefaultObjectStore
        {
            get
            {
                if (_defaultObjectStore == null)
                {
                    _defaultObjectStore = new ObjectStore();
                }

                return _defaultObjectStore;
            }
        }

        /// <summary>
        /// Interne function die eine Aktuell Eindeutige Zahl zurückgibt.
        /// Wird zur Ailias-Vergabe im OR-Mapper verwendet.
        /// </summary>
        /// <returns>Aktuell eindeutige Zahl</returns>
        internal static int CurrentUniqe()
        {
            if (_currentUniqe == int.MaxValue)
                _currentUniqe = 0;

            return _currentUniqe++;
        }
    }
}
