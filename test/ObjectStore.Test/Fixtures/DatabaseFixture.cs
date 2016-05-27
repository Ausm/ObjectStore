using ObjectStore.Interfaces;
using ObjectStore.OrMapping;
using ObjectStore.Test.Resources;
using ObjectStore.SqlClient;
using System;
using System.Data.SqlClient;
using System.IO;
using Xunit;
using System.Data.Common;
using System.Data;
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
