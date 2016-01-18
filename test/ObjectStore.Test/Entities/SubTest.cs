using System;

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
        public abstract int First { get; set; }

        [Mapping(FieldName = "[Second]")]
        public abstract int Second { get; set; }

        [Mapping(FieldName = "[Nullable]")]
        public abstract DateTime? Nullable { get; set; }
    }
}
