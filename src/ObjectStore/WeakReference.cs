using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectStore
{
    public class WeakReference<T> where T : class
    {
        WeakReference _reference;

        public WeakReference(T value)
        {
            _reference = new WeakReference(value);
        }

        public bool IsAlive 
        {
            get
            {
                return _reference.IsAlive && _reference.Target != null;
            }
        }

        public T Value
        {
            get
            {
                return _reference.IsAlive ? _reference.Target as T : null;
            }
        }

        public static implicit operator WeakReference<T> (T obj)
        {
            return new WeakReference<T>(obj);
        }

        public static implicit operator T(WeakReference<T> weakref)
        {
            return weakref.Value;
        }
    }
}
