using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectStore
{
    public interface ITableInfo
    {
        string TableName { get; }

        IEnumerable<string> FieldNames { get; }
    }
}
