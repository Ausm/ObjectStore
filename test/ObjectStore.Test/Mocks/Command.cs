using System;
using System.Data;
using System.Data.Common;

namespace ObjectStore.Test.Mocks
{
    public class Command : DbCommand
    {
        #region Fields
        string _commandText;
        ParameterCollection _parameterCollection = new ParameterCollection();
        Func<Command, DbDataReader> _getReader;
        DbConnection _connection;
        #endregion

        #region Constructors
        public Command(Func<Command, DbDataReader> getReader)
        {
            _getReader = getReader;
        }
        #endregion

        #region Overrides
        public override string CommandText
        {
            get
            {
                return _commandText;
            }

            set
            {
                _commandText = value;
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
                _connection = value;
            }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get
            {
                return _parameterCollection;
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
