using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectStore.Database
{
    public interface IDatabaseInitializer
    {
        void AddTable(string tableName);

        void AddField(string fieldname, Type type);

        void SetIsKeyField(bool isAutoIncrement);

        void AddForeignKey(string foreignTableName, string foreignKeyFieldName);

        void Flush();
    }
}
