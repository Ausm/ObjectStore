using System;

namespace ObjectStore.OrMapping
{
    public interface IValueSource : IDisposable
    {
        T GetValue<T>(string name);

        bool Next();
    }
}
