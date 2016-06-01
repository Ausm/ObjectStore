using ObjectStore.Interfaces;
using ObjectStore.OrMapping;
using ObjectStore.Test.Resources;
using ObjectStore.SqlClient;
using System;
using System.Data.Common;
using System.Reflection;
using ObjectStore.Test.Mocks;
using System.Text.RegularExpressions;

namespace ObjectStore.Test.Fixtures
{
    public class DatabaseFixture : IDisposable
    {
        #region Subclasses
        class HitCommandEventArgs : EventArgs
        {
            public HitCommandEventArgs(string key)
            {
                Key = key;
            }

            public string Key { get; }
        }
        #endregion

        #region Fields
        IObjectProvider _objectProvider;
        ResultManager<string> _resultManager;
        #endregion

        #region Constructor
        public DatabaseFixture()
        {
            if (_objectProvider == null)
                ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(_objectProvider = new RelationalObjectStore(Resource.MsSqlConnectionString, DataBaseProvider.Instance, true));

            Func<DbCommand> getCommand = () => new Command(GetReader);
            typeof(DataBaseProvider).GetTypeInfo().GetField("_getCommand", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static)
                .SetValue(null, getCommand);

            Func<string, DbConnection> getConnection = x => new Connection(x);
            typeof(DataBaseProvider).GetTypeInfo().GetField("_getConnection", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static)
                .SetValue(null, getConnection);


            _resultManager = new ResultManager<string>();
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
        public void AddSupportedQuery(string key, string pattern, string[] columnNames, params object[][] values)
        {
            _resultManager.AddItem(key, x => new Regex(pattern).IsMatch(x.CommandText), columnNames, values);
        }

        public T GetHitCount<T>(string key, Func<T> action, int expectedHitCount)
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
        public int GetHitCount<T>(string key, Action action)
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
            string key;

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
