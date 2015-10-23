using Microsoft.AspNet.Identity;
using Microsoft.Framework.DependencyInjection;
using System;
using System.Linq;

namespace CustomIdentity
{
    public static class Extensions
    {
        public static IdentityBuilder AddCustomUserStores<TUser, TRole>(this IdentityBuilder builder, IQueryable<TUser> users, IQueryable<TRole> roles,
            Func<TUser, string> getUserName, Func<TRole, string> getRoleName, Func<TUser, string> getPasswordHash, Func<TUser, TRole, bool> getIsUserInRole)
            where TUser : class
            where TRole : class
        {
            UserStore<TUser, TRole, string, string> userStore = new UserStore<TUser, TRole, string, string>(users, roles, getUserName, getUserName, getUserName, getPasswordHash, getRoleName, getRoleName, getRoleName, getIsUserInRole);

            builder.Services.Add(new ServiceDescriptor(typeof(UserStore<TUser, TRole, string, string>), userStore));
            builder.Services.Add(new ServiceDescriptor(typeof(IUserStore<TUser>), userStore));
            builder.Services.Add(new ServiceDescriptor(typeof(IRoleStore<TRole>), userStore));

            return builder;
        }
    }
}
