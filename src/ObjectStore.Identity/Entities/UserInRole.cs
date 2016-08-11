namespace ObjectStore.Identity
{
    [Table("dbo.UsersInRole")]
    public abstract class UserInRole<TUser, TRole>
    {
        [ForeignObjectMapping("User"), IsPrimaryKey]
        public abstract TUser User { get; set; }

        [ForeignObjectMapping("Role"), IsPrimaryKey]
        public abstract TRole Role { get; set; }
    }
}
