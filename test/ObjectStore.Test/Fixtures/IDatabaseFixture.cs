using ObjectStore.Interfaces;
using ObjectStore.Test.Tests;
using System;
using System.Collections.Generic;

namespace ObjectStore.Test.Fixtures
{
    public interface IDatabaseFixture
    {
        void InitializeSupportedQueries(Func<Query, IEnumerable<object[]>> getDefaultResult, Func<Query, string[]> getColumnNames, Func<Query, string> getPattern);

        IObjectProvider ObjectProvider { get; }

        void SetResult(Query key, IEnumerable<object[]> values);

        event EventHandler<HitCommandEventArgs> HitCommand;

    }

    public class HitCommandEventArgs : EventArgs
    {
        public HitCommandEventArgs(Query key)
        {
            Key = key;
        }

        public Query Key { get; }
    }

}
