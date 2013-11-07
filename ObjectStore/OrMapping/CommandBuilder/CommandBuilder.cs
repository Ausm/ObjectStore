using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Data;

namespace Ausm.ObjectStore.OrMapping
{
    public interface ICommandBuilder
    {
        void AddField(string fieldname, FieldType fieldtype);

        void AddField(string fieldname, object value, FieldType fieldtype, KeyInitializer keyInitializer);

        string Tablename { get; set; }
    }

    public interface ISqlCommandBuilder : ICommandBuilder
    {
        SqlCommand GetSqlCommand();
    }

    public interface IModifyableCommandBuilder : ICommandBuilder
    {
        void AddJoin(string tablename, string onClausel);

        List<SqlParameter> Parameters { get; set; }

        string Alias { get; }

        string WhereClausel { get; set; }

        void SetOrderBy(string expression);

        void SetTop(int count);
    }

    public interface ISubQueryCommandBuilder : ICommandBuilder
    {
        string SubQuery { get; }
    }

    public interface ISubQueryBuilder : IModifyableCommandBuilder
    {
        string Query { get; }
    }

    public enum FieldType
    {
        ReadOnlyField   = 0x0,
        InsertableField = 0x1,
        UpdateableField = 0x2,
        WriteableField  = 0x3,
        KeyField        = 0x4
    }
}
