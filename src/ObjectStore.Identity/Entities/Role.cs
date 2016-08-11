namespace ObjectStore.Identity
{
    [Table("dbo.Roles")]
    public abstract class Role
    {
        [Mapping(FieldName="Id"), IsPrimaryKey]
        public abstract int Id { get; }

        [Mapping(FieldName="Name")]
        public abstract string Name { get; set; }

        [Mapping(FieldName = "NormalizedRolename")]
        public abstract string NormalizedRolename { get; set; }
    }
}
