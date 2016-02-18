ObjectStore
===========

.Net Or-Mapper working with dynamically implemented abstract Classes

##How to Use?


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
