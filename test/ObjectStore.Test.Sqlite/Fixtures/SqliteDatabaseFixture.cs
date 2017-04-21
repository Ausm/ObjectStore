using ObjectStore.Interfaces;
using ObjectStore.OrMapping;
using ObjectStore.Sqlite;
using System;
using System.Data.Common;
using System.Reflection;
using ObjectStore.Test.Mocks;
using System.Text.RegularExpressions;
using ObjectStore.Test.Tests;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Linq;
using ObjectStore.Test.Fixtures;
using ObjectStore.MappingOptions;

namespace ObjectStore.Test.Sqlite
{
    public class SqliteDatabaseFixture : IDisposable, IDatabaseFixture
    {
        #region Fields
        IObjectProvider _objectProvider;
        ResultManager<Query> _resultManager;
        bool _isInitialized;
        #endregion

        #region Constructor
        public SqliteDatabaseFixture()
        {
            _isInitialized = false;

            RelationalObjectStore relationalObjectProvider = new RelationalObjectStore("Data Source=file::memory:?cache=shared;", DataBaseProvider.Instance, new MappingOptionsSet().AddDefaultRules(), true);
            ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(_objectProvider = relationalObjectProvider);
            relationalObjectProvider.Register<Entities.DifferentTypes>();
            relationalObjectProvider.Register<Entities.DifferentWritabilityLevels>();
            relationalObjectProvider.Register<Entities.ForeignObjectKey>();
            relationalObjectProvider.Register<Entities.NonInitializedKey>();
            relationalObjectProvider.Register<Entities.SubTest>();
            relationalObjectProvider.Register<Entities.Test>();
            relationalObjectProvider.InitializeDatabase();

            Func<DbCommand> getCommand = () => new Command(GetReader);
            typeof(DataBaseProvider).GetTypeInfo().GetField("_getCommand", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static)
                .SetValue(null, getCommand);

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

        public void Dispose()
        {
        }

        DbDataReader GetReader(Command command)
        {
            List<Query> keys = new List<Query>();

            DataReader returnValue = _resultManager.GetReader(command, keys);
            if (returnValue == null)
                throw new NotImplementedException($"No result for Query: \"{command.CommandText}\"");
            if(HitCommand != null)
                foreach(Query key in keys)
                    HitCommand(this, new HitCommandEventArgs(key));

            SqliteCommand sqliteCommand = new SqliteCommand(command.CommandText, (SqliteConnection)command.Connection);
            sqliteCommand.Parameters.AddRange(command.Parameters.Cast<SqliteParameter>());

            return sqliteCommand.ExecuteReader();
        }
        #endregion

        #region Events
        public event EventHandler<HitCommandEventArgs> HitCommand;
        #endregion
    }
}
