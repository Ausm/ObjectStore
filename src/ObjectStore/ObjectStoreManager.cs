using System;

namespace ObjectStore
{
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

        [Obsolete]
        internal static int CurrentUniqe()
        {
            if (_currentUniqe == int.MaxValue)
                _currentUniqe = 0;

            return _currentUniqe++;
        }
    }
}
