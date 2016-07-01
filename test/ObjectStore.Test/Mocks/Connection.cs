using System;
using System.Data;
using System.Data.Common;

namespace ObjectStore.Test.Mocks
{
    public class Connection : DbConnection
    {
        ConnectionState _connectionState = ConnectionState.Closed;
        string _connectionString;

        public Connection(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override string ConnectionString
        {
            get
            {
                return _connectionString;
            }

            set
            {
                _connectionString = value;
            }
        }

        public override string Database
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string DataSource
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string ServerVersion
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ConnectionState State
        {
            get
            {
                return _connectionState;
            }
        }

        public override void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            if (_connectionState == ConnectionState.Closed)
                throw new InvalidOperationException("Connection is already closed.");

            _connectionState = ConnectionState.Closed;
        }

        public override void Open()
        {
            if (_connectionState == ConnectionState.Open)
                throw new InvalidOperationException("Connection is already open.");

            _connectionState = ConnectionState.Open;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }

        protected override DbCommand CreateDbCommand()
        {
            throw new NotImplementedException();
        }
    }
}
