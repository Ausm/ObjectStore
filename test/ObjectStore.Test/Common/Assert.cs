using ObjectStore.Test.Fixtures;
using ObjectStore.Test.Tests;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ObjectStore.Test.Common
{
    internal class Assert : Xunit.Assert
    {
        public static void ScriptCalled(IDatabaseFixture fixture, Query key, Action action)
        {
            ScriptsCalled(fixture, action, 1, key);
        }

        public static T ScriptCalled<T>(IDatabaseFixture fixture, Query key, Func<T> func)
        {
            T returnValue = default(T);
            ScriptsCalled(fixture, () => returnValue = func(), 1, key);
            return returnValue;
        }

        public static void ScriptsCalled(IDatabaseFixture fixture, Action action, int expectedCount, Query key)
        {
            int hitCount = 0;
            EventHandler<HitCommandEventArgs> handler = (s, e) =>
            {
                if (e.Key == key)
                    hitCount++;
            };

            try
            {
                fixture.HitCommand += handler;
                action();
            }
            finally
            {
                fixture.HitCommand -= handler;
            }

            Equal(expectedCount, hitCount);
        }

        public static void ScriptsCalled(IDatabaseFixture fixture, Action action, params Query[] keys)
        {
            List<Query> keysList = keys.ToList();

            EventHandler<HitCommandEventArgs> handler = (s, e) =>
            {
                int index = keysList.IndexOf(e.Key);
                False(index == -1, "Wrong script executed.");
                keysList.RemoveAt(index);
            };

            try
            {
                fixture.HitCommand += handler;
                action();
            }
            finally
            {
                fixture.HitCommand -= handler;
            }

            Empty(keysList);
        }
    }
}
