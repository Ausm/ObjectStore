namespace ObjectStore.Test.Entities
{
    [Table("dbo.NonInitializedKey")]
    public abstract class NonInitializedKey
    {
        [Mapping(FieldName = "Id"), IsPrimaryKey]
        public abstract int Id { get; set; }
    }
}