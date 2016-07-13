namespace ObjectStore.Test.Entities
{
    [Table("dbo.DifferentWritabilityLevels")]
    public abstract class DifferentWritabilityLevels
    {
        [Mapping(FieldName = "Id"), IsPrimaryKey]
        public abstract int Id { get; }

        [Mapping(FieldName = "Writeable")]
        public abstract int Writeable { get; set; }

        [Mapping(FieldName = "Updateable", Insertable = false, Updateable = true)]
        public abstract int Updateable { get; set; }

        [Mapping(FieldName = "Insertable", Insertable = true, Updateable = false)]
        public abstract int Insertable { get; set; }

        [Mapping(FieldName = "Readonly")]
        public abstract int Readonly { get; }
    }
}
