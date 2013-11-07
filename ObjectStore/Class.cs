using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ausm
{
    public class Class<T1, T2>
    {
        T1 _a;
        T2 _b;

        public Class(T1 a, T2 b)
        {
            _a = a;
            _b = b;
        }

        public T1 A { get { return _a; } set { _a = value; } }
        public T2 B { get { return _b; } set { _b = value; } }
    }

    public class Class<T1, T2, T3>
    {
        T1 _a;
        T2 _b;
        T3 _c;

        public Class(T1 a, T2 b, T3 c)
        {
            _a = a;
            _b = b;
            _c = c;
        }

        public T1 A { get { return _a; } set { _a = value; } }
        public T2 B { get { return _b; } set { _b = value; } }
        public T3 C { get { return _c; } set { _c = value; } } 
    }
}
