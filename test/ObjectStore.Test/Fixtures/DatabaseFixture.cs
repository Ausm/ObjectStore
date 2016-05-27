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
        IObjectProvider _objectProvider;
        ResultManager<string> _resultManager;

        public DatabaseFixture()
        {
            if (_objectProvider == null)
                ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(_objectProvider = new RelationalObjectStore(Resource.MsSqlConnectionString, DataBaseProvider.Instance, true));

            Func<DbCommand> getCommand = () => new Command(GetReader);
#if DNXCORE50
            typeof(DataBaseProvider).GetField("_getCommand", BindingFlags.NonPublic | BindingFlags.Static)
#else
            typeof(DataBaseProvider).GetTypeInfo().GetField("_getCommand", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static)
#endif
                .SetValue(null, getCommand);

            Func<string, DbConnection> getConnection = x => new Connection(x);
#if DNXCORE50
            typeof(DataBaseProvider).GetField("_getConnection", BindingFlags.NonPublic | BindingFlags.Static)
#else
            typeof(DataBaseProvider).GetTypeInfo().GetField("_getConnection", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static)
#endif
                .SetValue(null, getConnection);


            _resultManager = new ResultManager<string>();
        }

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

        public void Dispose()
        {
        }

        DataReader GetReader(Command command)
        {
            DataReader returnValue = _resultManager.GetReader(command);
            if (returnValue == null)
                throw new NotImplementedException($"No result for Query: \"{command.CommandText}\"");

            return returnValue;
        }
        #endregion
    }
}
