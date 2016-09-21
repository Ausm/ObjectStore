using System.Collections.Generic;

namespace ObjectStore.Test.Entities
{
    [Table("dbo.ForeignObjectKeyTable")]
    public abstract class ForeignObjectKey
    {
        [ForeignObjectMapping("Id"), IsPrimaryKey]
        public abstract Test Id { get; set; }

        [Mapping(FieldName ="Value")]
        public abstract string Value { get; set; }
    }
}
