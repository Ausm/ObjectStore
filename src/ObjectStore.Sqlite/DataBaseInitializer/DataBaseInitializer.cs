using ObjectStore.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectStore.Sqlite
{
    public class DataBaseInitializer : IDatabaseInitializer
    {
        #region Subclasses
        class AddTableStatement
        {
            string _tablename;
            List<AddFieldStatment> _fieldStatements;

            public AddTableStatement(string tablename)
            {
                _tablename = tablename;
                _fieldStatements = new List<AddFieldStatment>();
            }

            public string Tablename => _tablename;

            public IEnumerable<AddFieldStatment> FieldStatements => _fieldStatements;

            public AddFieldStatment AddFieldStatement(string fieldname, Type type)
            {
                AddFieldStatment statement;
                _fieldStatements.Add(statement = new AddFieldStatment(this, fieldname, type));
                return statement;
            }
        }

        class AddFieldStatment
        {
            AddTableStatement _parentTableStatment;
            string _tableName;

            string _fieldname;
            Type _type;
            bool _isPrimaryKey;
            bool _isAutoincrement;

            string _foreignKeyTableName;
            string _foreignKeyFieldName;

            public AddFieldStatment(AddTableStatement addTableStatment, string fieldname, Type type) : this (fieldname, type)
            {
                _parentTableStatment = addTableStatment;
                _tableName = null;
            }

            public AddFieldStatment(string tableName, string fieldname, Type type) : this(fieldname, type)
            {
                _parentTableStatment = null;
                _tableName = tableName;
            }

            AddFieldStatment(string fieldname, Type type)
            {
                _fieldname = fieldname;
                _type = type;
                _isPrimaryKey = false;
                _isAutoincrement = false;
            }

            public string Fieldname => _fieldname;

            public Type Type => _type;

            public void SetForeignKey(string tableName, string fieldName)
            {
                _foreignKeyFieldName = fieldName;
                _foreignKeyTableName = tableName;
            }

            public void SetPrimaryKey(bool autoincrement)
            {
                _isPrimaryKey = true;
                _isAutoincrement = autoincrement;
            }
        }
        #endregion

        #region Fields
        string _connectionString;
        DataBaseProvider _databaseProvider;

        List<AddTableStatement> _tableStatments;
        List<AddFieldStatment> _fieldStatments;

        AddTableStatement _currentTableStatment;
        ITableInfo _currentTableInfo;
        AddFieldStatment _currentAddFieldStatment;
        #endregion

        #region Contructors
        public DataBaseInitializer(string connectionString, DataBaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider;
            _connectionString = connectionString;

            _tableStatments = new List<AddTableStatement>();
            _fieldStatments = new List<AddFieldStatment>();

            _currentTableInfo = null;
            _currentTableStatment = null;
        }
        #endregion

        #region IDatabaseInitializer member
        public void AddField(string fieldname, Type type)
        {
            if (_currentTableStatment != null)
            {
                _currentAddFieldStatment = _currentTableStatment.AddFieldStatement(fieldname, type);
            }
            else if (_currentTableInfo != null)
            {
                _fieldStatments.Add(new AddFieldStatment(_currentTableInfo.TableName, fieldname, type));
            }
            else
                throw new InvalidOperationException("No Table selected");
        }

        public void AddForeignKey(string foreignTableName, string foreignKeyFieldName)
        {
            _currentAddFieldStatment.SetForeignKey(foreignTableName, foreignKeyFieldName);
        }

        public void AddTable(string tableName)
        {
            _currentTableInfo = _databaseProvider.GetTableInfo(tableName, _connectionString);
            _currentTableStatment = _currentTableInfo == null ? new AddTableStatement(tableName) : null;
        }

        public void Flush()
        {
            _currentAddFieldStatment = null;
            _currentTableInfo = null;
            _currentTableStatment = null;

            // TODO
            throw new NotImplementedException();
        }

        public void SetIsKeyField(bool isAutoIncrement)
        {
            _currentAddFieldStatment.SetPrimaryKey(isAutoIncrement);
        }
        #endregion
    }
}
