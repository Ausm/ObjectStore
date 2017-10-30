using System.Collections.Generic;

namespace ObjectStore
{
    public interface ITableInfo
    {
        string TableName { get; }

        IEnumerable<string> FieldNames { get; }
    }
}
