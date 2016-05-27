using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectStore.Test.Common
{
    static class Extensions
    {
        public static bool ContainsInOrder(this string value, params string[] pattern)
        {
            int position = 0;
            for (int i = 0; i < pattern.Length; i++)
            {
                position = value.IndexOf(pattern[i], position);
                if (position == -1)
                    return false;

                position += pattern[i].Length;

                if (position > value.Length)
                    return false;
            }
            return true;
        }
    }
}
