namespace ObjectStore.Test.Entities
{
    [Table("dbo.SubTestTable")]
    public abstract class SubTest
    {
        [Mapping(FieldName = "Id"), IsPrimaryKey]
        public abstract int Id { get; }

        [ForeignObjectMapping("Test")]
        public abstract Test Test { get; set; }

        [Mapping(FieldName = "[Name]")]
        public abstract string Name { get; set; }

        [Mapping(FieldName = "[First]")]
        public int First { get; set; }

        [Mapping(FieldName = "[Second]")]
        public int Second { get; set; }
    }
}
