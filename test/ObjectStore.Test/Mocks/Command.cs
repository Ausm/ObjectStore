using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace ObjectStore.Test.Mocks
{
    class Command : DbCommand
    {
        #region Fields
        SqlCommand _innerCommand = new SqlCommand();
        Func<Command, DataReader> _getReader;
        Connection _connection;
        #endregion

        #region Constructors
        public Command(Func<Command ,DataReader> getReader)
        {
            _getReader = getReader;
        }
        #endregion

        #region Overrides
        public override string CommandText
        {
            get
            {
                return _innerCommand.CommandText;
            }

            set
            {
                _innerCommand.CommandText = value;
            }
        }

        public override int CommandTimeout
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override CommandType CommandType
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override bool DesignTimeVisible
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        protected override DbConnection DbConnection
        {
            get
            {
                return _connection;
            }

            set
            {
                _connection = value as Connection;
            }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get
            {
                return _innerCommand.Parameters;
            }
        }

        protected override DbTransaction DbTransaction
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Cancel()
        {
            throw new NotImplementedException();
        }

        public override int ExecuteNonQuery()
        {
            throw new NotImplementedException();
        }

        public override object ExecuteScalar()
        {
            throw new NotImplementedException();
        }

        public override void Prepare()
        {
            throw new NotImplementedException();
        }

        protected override DbParameter CreateDbParameter()
        {
            throw new NotImplementedException();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new InvalidOperationException("Connection is not open.");

            return _getReader(this);
        }
        #endregion
    }

}
