using ObjectStore.Interfaces;
using ObjectStore.OrMapping;
using ObjectStore.Test.Resources;
using ObjectStore.SqlClient;
using System;
using System.Data.Common;
using System.Reflection;
using ObjectStore.Test.Mocks;
using System.Text.RegularExpressions;
using ObjectStore.Test.Tests;
using System.Collections.Generic;
using System.Linq;

namespace ObjectStore.Test.Fixtures
{
    public class DatabaseFixture : IDisposable
    {
        #region Subclasses
        class HitCommandEventArgs : EventArgs
        {
            public HitCommandEventArgs(Query key)
            {
                Key = key;
            }

            public Query Key { get; }
        }
        #endregion

        #region Fields
        IObjectProvider _objectProvider;
        ResultManager<Query> _resultManager;
        bool _isInitialized;
        #endregion

        #region Constructor
        public DatabaseFixture()
        {
            _isInitialized = false;

            if (_objectProvider == null)
                ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(_objectProvider = new RelationalObjectStore(Resource.MsSqlConnectionString, DataBaseProvider.Instance, true));

            Func<DbCommand> getCommand = () => new Command(GetReader);
            typeof(DataBaseProvider).GetTypeInfo().GetField("_getCommand", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static)
                .SetValue(null, getCommand);

            Func<string, DbConnection> getConnection = x => new Connection(x);
            typeof(DataBaseProvider).GetTypeInfo().GetField("_getConnection", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static)
                .SetValue(null, getConnection);


            _resultManager = new ResultManager<Query>();
        }
        #endregion

        #region Properties
        public IObjectProvider ObjectProvider
        {
            get
            {
                return _objectProvider;
            }
        }
        #endregion

        #region Methods
        public void SetResult(Query key, IEnumerable<object[]> values)
        {
            _resultManager.SetValues(key, values);
        }

        public T GetHitCount<T>(Query key, Func<T> action, int expectedHitCount)
        {
            int hitCount = 0;
            EventHandler<HitCommandEventArgs> handler = (s, e) =>
            {
                if (e.Key == key)
                    hitCount++;
            };

            HitCommand += handler;
            T returnValue = action();
            HitCommand -= handler;

            Xunit.Assert.Equal(expectedHitCount, hitCount);
            return returnValue;
        }

        public void InitializeSupportedQueries(Func<Query, IEnumerable<object[]>> getDefaultResult, Func<Query, string[]> getColumnNames, Func<Query, string> getPattern)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            foreach (Query query in Enum.GetValues(typeof(Query)))
            {
                IEnumerable<object[]> values = getDefaultResult(query);
                _resultManager.AddItem(query, x => new Regex(getPattern(query)).IsMatch(x.CommandText), getColumnNames(query), getDefaultResult(query));
            }
        }

        public int GetHitCount<T>(Query key, Action action)
        {
            int hitCount = 0;
            EventHandler<HitCommandEventArgs> handler = (s, e) =>
            {
                if (e.Key == key)
                    hitCount++;
            };

            HitCommand += handler;
            action();
            HitCommand -= handler;

            return hitCount;
        }

        public void Dispose()
        {
        }

        DataReader GetReader(Command command)
        {
            Query key;

            DataReader returnValue = _resultManager.GetReader(command, out key);
            if (returnValue == null)
                throw new NotImplementedException($"No result for Query: \"{command.CommandText}\"");

            HitCommand?.Invoke(this, new HitCommandEventArgs(key));

            return returnValue;
        }
        #endregion

        #region Events
        event EventHandler<HitCommandEventArgs> HitCommand;
        #endregion
    }
}
