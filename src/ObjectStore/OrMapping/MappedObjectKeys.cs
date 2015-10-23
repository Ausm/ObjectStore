using System;

namespace ObjectStore.OrMapping
{
    public class MappedObjectKeys : System.Collections.IEnumerable
    {
        System.Collections.IEnumerable _keys;
        bool _isEmpty;

        public MappedObjectKeys(System.Collections.IEnumerable keys)
        {
            _keys = keys;
            _isEmpty = false;
        }

        public MappedObjectKeys()
        {
            _isEmpty = true;
        }

        public override int GetHashCode()
        {
            if (_isEmpty) return base.GetHashCode();
            foreach (object key in _keys)
            {
                return key.GetHashCode();
            }
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
                return true;
            if (_isEmpty)
                return false;

            if (obj.GetType() != typeof(MappedObjectKeys))
            {
                return false;
            }

            System.Collections.IEnumerator objKey = ((MappedObjectKeys)obj)._keys.GetEnumerator();
            System.Collections.IEnumerator thisKey = _keys.GetEnumerator();
            bool hasNextObj = objKey.MoveNext();
            bool hasNextThis = thisKey.MoveNext();
            while (hasNextObj && hasNextThis)
            {
                if (thisKey.Current == null)
                {
                    if (objKey.Current != null)
                        return false;
                }
                else if (!thisKey.Current.Equals(objKey.Current))
                    return false;
                hasNextObj = objKey.MoveNext();
                hasNextThis = thisKey.MoveNext();
            }

            return hasNextObj == hasNextThis;
        }

        public void SetKeys(System.Collections.IEnumerable keys)
        {
            if (_isEmpty)
            {
                _keys = keys;
                _isEmpty = false;
            }
        }

        public object Single()
        {
            System.Collections.IEnumerator enumerator = _keys.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new Exception("Key is empty.");

            object returnValue = enumerator.Current;
            if(enumerator.MoveNext())
                throw new Exception("Single key can only be aquiered when the key is not segmented.");

            return returnValue;
        }

        public int Count
        {
            get
            {
                if (_isEmpty)
                    return 0;
                int i = 0;
                for (System.Collections.IEnumerator enumerator = _keys.GetEnumerator(); enumerator.MoveNext(); i++) { }
                return i;
            }
        }

        #region IEnumerable Members

        public System.Collections.IEnumerator GetEnumerator()
        {
            return _keys.GetEnumerator();
        }

        #endregion
    }
}
