using System;

namespace ObjectStore.Database
{
    public interface IValueSource : IDisposable
    {
        T GetValue<T>(string name);

        bool Next();
    }
}
