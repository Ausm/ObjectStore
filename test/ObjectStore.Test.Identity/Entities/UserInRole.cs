using O = ObjectStore.Identity;

namespace ObjectStore.Test.Identity.Entities
{
    public abstract class UserInRole : O.UserInRole<User, Role>
    {
    }
}
