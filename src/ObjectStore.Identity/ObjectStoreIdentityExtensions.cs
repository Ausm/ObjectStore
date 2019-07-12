using System;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Linq.Expressions;
using ObjectStore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ObjectStore.Identity
{
    public static class ObjectStoreIdentityExtensions
    {
        public static IdentityBuilder AddObjectStoreUserStores<TUser, TRole, TUserInRole>(this IdentityBuilder builder)
            where TUser : User
            where TRole : Role
            where TUserInRole : UserInRole<TUser, TRole>
        {

            AddObjectStoreUserStores<TUser, TRole, TUserInRole>(builder, options =>
            {
                options.UserIdProperty = GetPropertyInfo((TUser x) => x.Id);
                options.UserNameProperty = GetPropertyInfo((TUser x) => x.Name);
                options.NormalizedUsernameProperty = GetPropertyInfo((TUser x) => x.NormalizedUsername);
                options.NormalizedRolenameProperty = GetPropertyInfo((TRole x) => x.NormalizedRolename);
                options.RoleNameProperty = GetPropertyInfo((TRole x) => x.Name);
                options.PasswordHashProperty = GetPropertyInfo((TUser x) => x.Password);
                options.UserProperty = GetPropertyInfo((TUserInRole x) => x.User);
                options.RoleProperty = GetPropertyInfo((TUserInRole x) => x.Role);
                options.ConvertIdToString = x => x?.ToString();
                options.ConvertStringToId = x => {
                    int value;
                    if (int.TryParse(x, out value))
                        return value;

                    return null;
                };
            });

            return builder;
        }

        public static IdentityBuilder AddObjectStoreUserStores(this IdentityBuilder builder)
        {
            return AddObjectStoreUserStores<User, Role, UserInRole<User, Role>>(builder);
        }

        public static IdentityBuilder AddObjectStoreUserStores<TUser, TRole, TUserInRole>(this IdentityBuilder builder, Action<UserStoreOptions> configure)
            where TUser : class
            where TRole : class
            where TUserInRole : class
        {
            ServiceDescriptor objectProviderDescriptor = builder.Services.Where(x => x.ServiceType == typeof(IObjectProvider)).FirstOrDefault();
            if (objectProviderDescriptor == null)
                builder.Services.Add(new ServiceDescriptor(typeof(IObjectProvider), x => ObjectStoreManager.DefaultObjectStore, ServiceLifetime.Transient));

            builder.Services.Configure(configure);

            builder.Services.Add(new ServiceDescriptor(typeof(UserStore<TUser, TRole, TUserInRole>), typeof(UserStore<TUser, TRole, TUserInRole>), ServiceLifetime.Transient));
            builder.Services.Add(new ServiceDescriptor(typeof(IUserStore<TUser>), typeof(UserStore<TUser, TRole, TUserInRole>), ServiceLifetime.Transient));
            builder.Services.Add(new ServiceDescriptor(typeof(IRoleStore<TRole>), typeof(RoleStore<TRole>), ServiceLifetime.Transient));

            return builder;
        }

        static PropertyInfo GetPropertyInfo<T, TResult>(Expression<Func<T, TResult>> propertyAccessExpression)
            => ((propertyAccessExpression.Body as MemberExpression)?.Member is PropertyInfo propertyInfo) ? propertyInfo : 
                throw new ArgumentException("Expression must be a property access expression", "expression");
    }
}
