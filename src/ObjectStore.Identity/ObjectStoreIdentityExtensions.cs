using System;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Linq.Expressions;
using ObjectStore.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ObjectStore.Identity
{
    public static class ObjectStoreIdentityExtensions
    {
        public static IdentityBuilder AddObjectStoreUserStores<TUser, TRole>(this IdentityBuilder builder, Expression<Func<TUser, string>> getUserName, Expression<Func<TUser, string>> getNormalizedUserName, Expression<Func<TRole, string>> getRoleName, Expression<Func<TUser, string>> getPasswordHash, Func<TUser, TRole, bool> getIsUserInRole)
            where TUser : class
            where TRole : class
        {
            AddObjectStoreUserStores<TUser, TRole, string, string>(builder, options =>
            {
                options.SetUserNameProperty(getUserName);
                options.SetUserNormalizedUsernameProperty(getNormalizedUserName);
                options.SetRoleNameProperty(getRoleName);
                options.SetUserPasswordHashProperty(getPasswordHash);
                options.GetIsUserInRole = getIsUserInRole;
            });

            return builder;
        }

        public static IdentityBuilder AddObjectStoreUserStores<TUser, TRole>(this IdentityBuilder builder, Action<UserStoreOptions<TUser, TRole, string, string>> configure)
            where TUser : class
            where TRole : class
        {
            AddObjectStoreUserStores<TUser, TRole, string, string>(builder, configure);
            return builder;
        }

        public static IdentityBuilder AddObjectStoreUserStores<TUser, TRole>(this IdentityBuilder builder)
            where TUser : User
            where TRole : Role
        {
            return AddObjectStoreUserStores<User, Role>(builder, 
                x => x.Name, 
                x => x.NormalizedUsername,
                x => x.Name, 
                x => x.Password, 
                (user, role) => true);
        }

        public static IdentityBuilder AddObjectStoreUserStores(this IdentityBuilder builder)
        {
            return AddObjectStoreUserStores<User, Role>(builder);
        }


        public static IdentityBuilder AddObjectStoreUserStores<TUser, TRole, TUserKey, TRoleKey>(this IdentityBuilder builder, Action<UserStoreOptions<TUser, TRole, TUserKey, TRoleKey>> configure)
            where TUser : class
            where TRole : class
            where TUserKey : IEquatable<TUserKey>
            where TRoleKey : IEquatable<TRoleKey>
        {
            ServiceDescriptor objectProviderDescriptor = builder.Services.Where(x => x.ServiceType == typeof(IObjectProvider)).FirstOrDefault();
            if (objectProviderDescriptor == null)
                builder.Services.Add(new ServiceDescriptor(typeof(IObjectProvider), x => ObjectStoreManager.DefaultObjectStore, ServiceLifetime.Transient));

            builder.Services.Configure<UserStoreOptions<TUser, TRole, TUserKey, TRoleKey>>(configure);

            builder.Services.Add(new ServiceDescriptor(typeof(UserStore<TUser, TRole, TUserKey, TRoleKey>), typeof(UserStore<TUser, TRole, string, string>), ServiceLifetime.Transient));
            builder.Services.Add(new ServiceDescriptor(typeof(IUserStore<TUser>), typeof(UserStore<TUser, TRole, string, string>), ServiceLifetime.Transient));
            builder.Services.Add(new ServiceDescriptor(typeof(IRoleStore<TRole>), typeof(UserStore<TUser, TRole, string, string>), ServiceLifetime.Transient));

            return builder;
        }
    }
}
