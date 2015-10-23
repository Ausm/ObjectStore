using System;
using System.Linq;

namespace ObjectStore.Interfaces
{
    public interface IObjectProvider
    {
        IQueryable<T> GetQueryable<T>() where T : class;

        T CreateObject<T>() where T : class;

        bool SupportsType(Type type);
    }
}
