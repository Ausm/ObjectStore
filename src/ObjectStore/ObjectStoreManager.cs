using System;

namespace ObjectStore
{
    public static class ObjectStoreManager
    {
        static ObjectStore _defaultObjectStore = null;

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
    }
}
