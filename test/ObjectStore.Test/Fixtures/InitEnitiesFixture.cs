using ObjectStore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectStore.Test.Fixtures
{
    public class InitEnitiesFixture : IDisposable
    {
        bool _isInitialized;

        public InitEnitiesFixture()
        {
            _isInitialized = false;
        }

        public void Init(IObjectProvider objectProvider)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            int currentName = 0;

            Entities.Test firstEntity = CreateTestEntity(objectProvider, "First", Resources.Resource.FirstRandomText);
            Entities.Test secondEntity = CreateTestEntity(objectProvider, "Second", Resources.Resource.SecondRandomText);
            for (int i = 0; i < 10; i++)
            {
                CreatSubTestEntity(objectProvider, firstEntity, $"SubEntity{currentName++}", i, 10 - i, i != 7 ? default(DateTime?) : DateTime.Now);
                CreatSubTestEntity(objectProvider, secondEntity, $"SubEntity{currentName++}", i, 10 - i, i != 7 ? default(DateTime?) : DateTime.Now);
            }
            objectProvider.GetQueryable<Entities.Test>().Save();
        }

        static Entities.Test CreateTestEntity(IObjectProvider objectProvider, string name, string description)
        {
            Entities.Test entity = objectProvider.CreateObject<Entities.Test>();
            entity.Name = name;
            entity.Description = description;
            return entity;
        }

        static Entities.SubTest CreatSubTestEntity(IObjectProvider objectProvider, Entities.Test parent, string name, int first, int second, DateTime? date)
        {
            Entities.SubTest entity = objectProvider.CreateObject<Entities.SubTest>();
            entity.Name = name;
            entity.Test = parent;
            entity.First = first;
            entity.Second = second;
            entity.Nullable = date;
            return entity;
        }

        public void Dispose()
        {
        }
    }
}
