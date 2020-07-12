namespace ObjectStore.Test.Entities
{
    [Table("dbo.ForeignObjectKeyChainTable")]
    public abstract class ForeignObjectKeyChain
    {
        [ForeignObjectMapping("Id"), IsPrimaryKey]
        public abstract ForeignObjectKey ForeignObjectKey { get; set; }

        [ForeignObjectMapping("SubTest"), IsPrimaryKey]
        public abstract SubTest SubTest { get; set; }

        [Mapping(FieldName ="Value")]
        public abstract string Value { get; set; }
    }
}
