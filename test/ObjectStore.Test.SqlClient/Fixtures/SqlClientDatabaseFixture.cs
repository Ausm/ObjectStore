﻿using ObjectStore.Interfaces;
using ObjectStore.OrMapping;
using ObjectStore.SqlClient;
using System;
using System.Data.Common;
using System.Reflection;
using ObjectStore.Test.Mocks;
using System.Text.RegularExpressions;
using ObjectStore.Test.Tests;
using System.Collections.Generic;
using ObjectStore.Test.Fixtures;
using System.Linq;
using ObjectStore.MappingOptions;

namespace ObjectStore.Test.SqlClient
{
    public class SqlClientDatabaseFixture : IDisposable, IDatabaseFixture
    {
        #region Fields
        IObjectProvider _objectProvider;
        ResultManager<Query> _resultManager;
        bool _isInitialized;
        #endregion

        #region Constructor
        public SqlClientDatabaseFixture()
        {
            _isInitialized = false;

            if (_objectProvider == null)
                ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(_objectProvider = new RelationalObjectStore("data source=(local);Integrated Security=True;initial catalog=Test", DataBaseProvider.Instance, new MappingOptionsSet().AddDefaultRules(), true));

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

            //if (keys.Count == 1 && keys[0] == Query.)
            //{
            //    System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection("Data Source=.;Initial Catalog=Test;Trusted_Connection=True;");
            //    connection.Open();
            //    System.Data.SqlClient.SqlCommand sqlCommand = new System.Data.SqlClient.SqlCommand(command.CommandText, connection);
            //    sqlCommand.Parameters.AddRange(command.Parameters.Cast<DbParameter>().ToArray());
            //    return sqlCommand.ExecuteReader();
            //}

            return returnValue;
        }
        #endregion

        #region Events
        public event EventHandler<HitCommandEventArgs> HitCommand;
        #endregion
    }
}
