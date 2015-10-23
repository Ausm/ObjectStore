using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Reflection;
using System.Linq.Expressions;
using System.Linq;

namespace ObjectStore.OrMapping
{
    public interface IObjectStoreQueryable<T> : IOrderedQueryable<T>
    {
    }
}
