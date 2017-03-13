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

            public void AddFieldStatement(string fieldname, Type type, bool isPrimaryKey, bool isAutoincrement)
            {
                if (isPrimaryKey)
                    _fieldStatements.Insert(0, new AddFieldStatment(this, fieldname, type, isPrimaryKey, isAutoincrement));
                else
                    _fieldStatements.Add(new AddFieldStatment(this, fieldname, type, isPrimaryKey, isAutoincrement));
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

            public AddFieldStatment(AddTableStatement addTableStatment, string fieldname, Type type, bool isPrimaryKey, bool isAutoincrement) : this (fieldname, type, isPrimaryKey, isAutoincrement)
            {
                _parentTableStatment = addTableStatment;
                _tableName = null;
            }

            public AddFieldStatment(string tableName, string fieldname, Type type, bool isPrimaryKey, bool isAutoincrement) : this(fieldname, type, isPrimaryKey, isAutoincrement)
            {
                _parentTableStatment = null;
                _tableName = tableName;
            }

            AddFieldStatment(string fieldname, Type type, bool isPrimaryKey, bool isAutoincrement)
            {
                _fieldname = fieldname;
                _type = type;
                _isPrimaryKey = isPrimaryKey;
                _isAutoincrement = isAutoincrement;
            }

            public string Fieldname => _fieldname;

            public Type Type => _type;

            public void SetForeignKey(string tableName, string fieldName)
            {
                _foreignKeyFieldName = fieldName;
                _foreignKeyTableName = tableName;
            }
        }
        #endregion

        #region Fields
        string _connectionString;
        DataBaseProvider _databaseProvider;

        List<AddTableStatement> _tableStatments;
        List<AddFieldStatment> _fieldStatments;

        AddTableStatement _currentTableStatment;
        string _currentTableName;
        #endregion

        #region Contructors
        public DataBaseInitializer(string connectionString, DataBaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider;
            _connectionString = connectionString;

            _tableStatments = new List<AddTableStatement>();
            _fieldStatments = new List<AddFieldStatment>();

            _currentTableName = null;
            _currentTableStatment = null;
        }
        #endregion

        #region IDatabaseInitializer member
        public void AddField(string fieldname, Type type)
        {
            throw new NotImplementedException();
        }

        public void AddForeignKey(string foreignTableName, string foreignKeyFieldName)
        {
            throw new NotImplementedException();
        }

        public void AddTable(string tableName)
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void SetIsKeyField(bool isAutoIncrement)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
