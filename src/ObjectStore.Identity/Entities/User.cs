namespace ObjectStore.Identity
{
    [Table("dbo.Users")]
    public abstract class User
    {
        [Mapping(FieldName="Id"), IsPrimaryKey]
        public abstract int Id { get; }

        [Mapping(FieldName="Name")]
        public abstract string Name { get; set; }

        [Mapping(FieldName="NormalizedUsername")]
        public abstract string NormalizedUsername { get; set; }

        [Mapping(FieldName="Password")]
        public abstract string Password { get; set; }
    }
}
