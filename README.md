ObjectStore 
===========
Travis CI:
[![Build Status](https://travis-ci.org/Ausm/ObjectStore.svg?branch=master)](https://travis-ci.org/Ausm/ObjectStore)
Appveyor:
[![Build status](https://ci.appveyor.com/api/projects/status/9r1b0mfgjskf7gry/branch/master?svg=true)](https://ci.appveyor.com/project/Ausm/objectstore/branch/master)
Nuget:
[![NuGet Pre Release](https://img.shields.io/nuget/vpre/ObjectStore.svg?maxAge=2592000?style=plastic)](https://www.nuget.org/packages/ObjectStore)

.Net Or-Mapper working with dynamically implemented abstract Classes

## How to Use?


**Simply write classes like:**


```C#
[Table("Entity1")]
public abstract class Entity1
{
    [Mapping(FieldName="EntityId"), IsPrimaryKey]
    public abstract int Id { get; }

    [Mapping(FieldName = "TextField")]
    public abstract string Text { get; set; }

    [ForeignObjectMapping("Entity2Id")]
    public abstract Entity2 Entity2 { get; set; }

    [ReferenceListMapping(typeof(Entity3), "Entity1PropertyName")]
    public abstract ICollection<Entity3> SubEntities { get; }
}
```

**Initialize with:**
`ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(new RelationalObjectStore("connectionString", true));`

**Call:**
`ObjectStoreManager.DefaultObjectStore.GetQueryable<Entity1>();`
to get Entity-Objects derived from the given Class, with INotifyPropertyChanged implemented...
