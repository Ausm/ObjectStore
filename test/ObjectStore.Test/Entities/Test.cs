using System.Collections.Generic;

namespace ObjectStore.Test.Entities
{
    [Table("dbo.TestTable")]
    public abstract class Test
    {
        [Mapping(FieldName = "Id"), IsPrimaryKey]
        public abstract int Id { get; }

        [Mapping(FieldName ="[Name]")]
        public abstract string Name { get; set; }

        [Mapping(FieldName ="[Description]")]
        public abstract string Description { get; set; }

        [ReferenceListMapping(typeof(SubTest), nameof(SubTest.Test))]
        public abstract ICollection<SubTest> SubTests { get; }

        [ReferenceListMapping(typeof(ForeignObjectKey), nameof(ForeignObjectKey.Test))]
        public abstract ICollection<ForeignObjectKey> ForeignObjectKeyValue { get; }
    }
}
