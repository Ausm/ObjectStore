using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ObjectStore.Identity
{
    public class UserStoreOptions
    {
        public PropertyInfo RoleIdProperty { get; set; }
        public PropertyInfo RoleNameProperty { get; set; }
        public PropertyInfo UserIdProperty { get; set; }
        public PropertyInfo UserNameProperty { get; set; }
        public PropertyInfo PasswordHashProperty { get; set; }
        public PropertyInfo NormalizedUsernameProperty { get; set; }
        public PropertyInfo NormalizedRolenameProperty { get; set; }
        public PropertyInfo UserProperty { get; set; }
        public PropertyInfo RoleProperty { get; set; }
        public Func<object, string> ConvertIdToString { get; set; }
        public Func<string, object> ConvertStringToId { get; set; }
    }
}
