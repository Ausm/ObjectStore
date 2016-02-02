using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace ObjectStore.OrMapping
{
    public interface ICommandBuilder
    {
        void AddField(string fieldname, FieldType fieldtype);

        void AddField(string fieldname, object value, FieldType fieldtype, Type keyInitializerType, bool isChanged);

        string Tablename { get; set; }
        DbCommand GetDbCommand();
    }

    public interface IModifyableCommandBuilder : ICommandBuilder
    {
        void SetWhereClausel(LambdaExpression expression);

        void SetOrderBy(LambdaExpression expression);

        void SetTop(int count);
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
