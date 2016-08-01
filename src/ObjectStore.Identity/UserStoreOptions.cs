using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ObjectStore.Identity
{
    public class UserStoreOptions<TUser, TRole, TUserKey, TRoleKey>
    {
        public void SetUserNameProperty<T>(Expression<Func<TUser, T>> expression)
        {
            SetUserNameProperty(GetPropertyInfo(expression));
        }
        public void SetUserNameProperty(PropertyInfo property)
        {

            ParameterExpression userParameter = Expression.Parameter(typeof(TUser));
            GetUserName = Expression.Lambda<Func<TUser, string>>(Expression.Property(userParameter, property), userParameter).Compile();
            if (GetUserId == null)
                GetUserId = GetUserName;
            if (GetUserKey == null && typeof(TUserKey) == typeof(string))
                GetUserKey = (Func<TUser, TUserKey>)((object)GetUserName);

            userParameter = Expression.Parameter(typeof(TUser));
            ParameterExpression nameParameter = Expression.Parameter(typeof(string));
            SetUserName = Expression.Lambda<Action<TUser, string>>(Expression.Call(userParameter, property.GetSetMethod(), nameParameter), userParameter, nameParameter).Compile();
        }
        public void SetRoleNameProperty<T>(Expression<Func<TRole, T>> expression)
        {
            SetRoleNameProperty(GetPropertyInfo(expression));
        }
        public void SetRoleNameProperty(PropertyInfo property)
        {
            ParameterExpression roleParameter = Expression.Parameter(typeof(TRole));
            GetRoleName = Expression.Lambda<Func<TRole, string>>(Expression.Property(roleParameter, property), roleParameter).Compile();
            if (GetRoleId == null)
                GetRoleId = GetRoleName;
            if (GetRoleKey == null && typeof(TUserKey) == typeof(string))
                GetRoleKey = (Func<TRole, TRoleKey>)((object)GetRoleName);

            roleParameter = Expression.Parameter(typeof(TRole));
            ParameterExpression nameParameter = Expression.Parameter(typeof(string));
            SetRoleName = Expression.Lambda<Action<TRole, string>>(Expression.Call(roleParameter, property.GetSetMethod(), nameParameter), roleParameter, nameParameter).Compile();
        }
        public void SetUserPasswordHashProperty(PropertyInfo property)
        {
            ParameterExpression userParameter = Expression.Parameter(typeof(TUser));
            GetUserPasswordHash = Expression.Lambda<Func<TUser, string>>(Expression.Property(userParameter, property), userParameter).Compile();

            userParameter = Expression.Parameter(typeof(TUser));
            ParameterExpression passwordParameter = Expression.Parameter(typeof(string));
            SetUserPasswordHash = Expression.Lambda<Action<TUser, string>>(Expression.Call(userParameter, property.GetSetMethod(), passwordParameter), userParameter, passwordParameter).Compile();
        }
        public void SetUserPasswordHashProperty<T>(Expression<Func<TUser, T>> expression)
        {
            SetUserPasswordHashProperty(GetPropertyInfo(expression));
        }
        public void SetUserNormalizedUsernameProperty(PropertyInfo property)
        {
            ParameterExpression userParameter = Expression.Parameter(typeof(TUser));
            GetUserNormalizedUsername = Expression.Lambda<Func<TUser, string>>(Expression.Property(userParameter, property), userParameter).Compile();

            userParameter = Expression.Parameter(typeof(TUser));
            ParameterExpression normalizedUsernameParameter = Expression.Parameter(typeof(string));
            SetUserNormalizedUsername = Expression.Lambda<Action<TUser, string>>(Expression.Call(userParameter, property.GetSetMethod(), normalizedUsernameParameter), userParameter, normalizedUsernameParameter).Compile();
        }
        public void SetUserNormalizedUsernameProperty<T>(Expression<Func<TUser, T>> expression)
        {
            SetUserNormalizedUsernameProperty(GetPropertyInfo(expression));
        }

        public Func<TUser, TUserKey> GetUserKey { get; set; }
        public Func<TRole, TRoleKey> GetRoleKey { get; set; }
        public Func<TUser, string> GetUserId { get; set; }
        public Func<TRole, string> GetRoleId { get; set; }
        public Func<TRole, string> GetRoleName { get; set; }
        public Action<TRole, string> SetRoleName { get; set; }
        public Func<TUser, string> GetUserName { get; set; }
        public Action<TUser, string> SetUserName { get; set; }
        public Func<TUser, string> GetUserPasswordHash { get; set; }
        public Action<TUser, string> SetUserPasswordHash { get; set; }
        public Func<TUser, string> GetUserNormalizedUsername { get; set; }
        public Action<TUser, string> SetUserNormalizedUsername { get; set; }
        public Func<TUser, TRole, bool> GetIsUserInRole { get; set; }

        static PropertyInfo GetPropertyInfo<T, TResult>(Expression<Func<T, TResult>> propertyAccessExpression)
        {
            PropertyInfo propertyInfo = (propertyAccessExpression.Body as MemberExpression)?.Member as PropertyInfo;

            if (propertyInfo == null)
                throw new ArgumentException("Expression must be a property access expression", "expression");

            return propertyInfo;
        }
    }
}
