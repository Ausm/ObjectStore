﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Identity;
using ObjectStore.Interfaces;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Linq.Expressions;

namespace ObjectStore.Identity
{
    public class UserStore<TUser, TRole, TUserInRole> :
        IUserRoleStore<TUser>,
        IUserPasswordStore<TUser>,
        IQueryableUserStore<TUser>
        where TUser : class
        where TRole : class
        where TUserInRole : class
    {
        #region Fields
        static Dictionary<PropertyInfo, Delegate> _setPropertyFunctions;
        static Dictionary<PropertyInfo, Delegate> _getPropertyFunctions;

        IObjectProvider _objectProvider;
        IRoleStore<TRole> _roleStore;
        IQueryable<TUser> _users;
        IQueryable<TUserInRole> _usersInRole;
        UserStoreOptions _options;
        #endregion

        #region Constructors
        static UserStore()
        {
            _setPropertyFunctions = new Dictionary<PropertyInfo, Delegate>();
            _getPropertyFunctions = new Dictionary<PropertyInfo, Delegate>();
        }
        public UserStore(IObjectProvider objectProvider, IOptions<UserStoreOptions> options, IRoleStore<TRole> roleStore)
        {
            _objectProvider = objectProvider;
            _users = objectProvider.GetQueryable<TUser>();
            _roleStore = roleStore;
            _usersInRole = objectProvider.GetQueryable<TUserInRole>();
            _options = options.Value;
        }
        #endregion

        #region Properties
        public IQueryable<TUser> Users
        {
            get
            {
                return _users;
            }
        }
        #endregion

        #region Methods
        public async Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            TRole role = await _roleStore.FindByNameAsync(roleName, cancellationToken);
            if (role == null)
                return;

            TUserInRole userInRole = _objectProvider.CreateObject<TUserInRole>();
            SetProperty(userInRole, _options.RoleProperty, role);
            SetProperty(userInRole, _options.UserProperty, user);
            _usersInRole.Where(x => x == userInRole).Save();
        }
        public async Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            TRole role = await _roleStore.FindByNameAsync(roleName, cancellationToken);
            if (role == null)
                return;

            IQueryable<TUserInRole> userInRoles = _usersInRole
                .Where(GetPredicat<TUserInRole, TUser>(_options.UserProperty, user))
                .Where(GetPredicat<TUserInRole, TRole>(_options.RoleProperty, role));
            userInRoles.Delete();
            userInRoles.Save();
        }
        public Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
        {
            TUser newUser = _objectProvider.CreateObject<TUser>();
            SetProperty(newUser, _options.UserNameProperty, GetProperty<TUser, string>(user, _options.UserNameProperty));
            SetProperty(newUser, _options.NormalizedUsernameProperty, GetProperty<TUser, string>(user, _options.NormalizedUsernameProperty));
            SetProperty(newUser, _options.PasswordHashProperty, GetProperty<TUser, string>(user, _options.PasswordHashProperty));
            _users.Where(x => x == newUser).Save();
            return Task.FromResult(IdentityResult.Success);
        }
        public Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
        {
            IQueryable<TUser> users = _users.Where(x => x == user);
            users.Delete();
            users.Save();
            return Task.FromResult(IdentityResult.Success);
        }
        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(GetProperty<TUser, string>(user, _options.NormalizedUsernameProperty));
        }
        public Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken)
        {
            SetProperty(user, _options.NormalizedUsernameProperty, normalizedName);
            return Task.FromResult(true);
        }
        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(GetProperty<TUser, string>(user, _options.PasswordHashProperty));
        }
        public async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
        {
            IQueryable<TUserInRole> userInRoles = _usersInRole.Where(GetPredicat<TUserInRole, TUser>(_options.UserProperty, user));
            await userInRoles.FetchAsync();

            List<string> returnValue = new List<string>();

            foreach (TUserInRole userInRole in userInRoles)
            {
                TRole role = GetProperty<TUserInRole, TRole>(userInRole, _options.RoleProperty);
                returnValue.Add(GetProperty<TRole, string>(role, _options.RoleNameProperty));
            }

            return returnValue;
        }
        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(_options.ConvertIdToString(GetProperty<TUser, object>(user, _options.UserIdProperty)));
        }
        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(GetProperty<TUser, string>(user, _options.UserNameProperty));
        }
        public async Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            TRole role = await _roleStore.FindByNameAsync(roleName, cancellationToken);

            if (role == null)
                return new List<TUser>();

            IQueryable<TUserInRole> userInRoles = _usersInRole.Where(GetPredicat<TUserInRole, TRole>(_options.RoleProperty, role));
            await userInRoles.FetchAsync();

            List<TUser> returnValue = new List<TUser>();
            foreach (TUserInRole userInRole in userInRoles)
                returnValue.Add(GetProperty<TUserInRole, TUser>(userInRole, _options.UserProperty));

            return returnValue;
        }
        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(GetProperty<TUser, string>(user, _options.PasswordHashProperty)));
        }
        public async Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            TRole role = await _roleStore.FindByNameAsync(roleName, cancellationToken);

            if (role == null)
                return false;


            IQueryable<TUserInRole> userInRoles = _usersInRole.Where(GetPredicat<TUserInRole, TUser>(_options.UserProperty, user))
                                                    .Where(GetPredicat<TUserInRole, TRole>(_options.RoleProperty, role));
            await userInRoles.FetchAsync();

            return userInRoles.Any();
        }
        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken)
        {
            SetProperty(user, _options.PasswordHashProperty, passwordHash);
            return Task.FromResult(true);
        }
        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken)
        {
            SetProperty(user, _options.UserNameProperty, userName);
            return Task.FromResult(true);
        }
        public Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
        {
            _users.Where(x => x == user).Save();
            return Task.FromResult(IdentityResult.Success);
        }
        public async Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            object convertedId = _options.ConvertStringToId(userId);

            IQueryable<TUser> users = _users.Where(GetPredicat<TUser, object>(_options.UserIdProperty, convertedId));
            await users.FetchAsync();

            return users.FirstOrDefault();
        }
        public async Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            IQueryable<TUser> users = _users.Where(GetPredicat<TUser, string>(_options.NormalizedUsernameProperty, normalizedUserName));
            await users.FetchAsync();

            return users.FirstOrDefault();
        }

        #region Static
        static void SetProperty<TInstance, TValue>(TInstance instance, PropertyInfo propertyInfo, TValue value )
        {
            Action<TInstance, TValue> func;
            if (_setPropertyFunctions.ContainsKey(propertyInfo))
                func = (Action<TInstance, TValue>)_setPropertyFunctions[propertyInfo];
            else
            {
                ParameterExpression instanceParameter = Expression.Parameter(typeof(TInstance));
                ParameterExpression valueParameter = Expression.Parameter(typeof(TValue));
                func = Expression.Lambda<Action<TInstance, TValue>>(Expression.Call(instanceParameter, propertyInfo.GetSetMethod(), valueParameter), instanceParameter, valueParameter).Compile();
                _setPropertyFunctions.Add(propertyInfo, func);
            }

            func(instance, value);
        }

        static TValue GetProperty<TInstance, TValue>(TInstance instance, PropertyInfo propertyInfo)
        {
            Func<TInstance, TValue> func;
            if (_getPropertyFunctions.ContainsKey(propertyInfo))
                func = (Func<TInstance, TValue>)_getPropertyFunctions[propertyInfo];
            else
            {
                ParameterExpression instanceParameter = Expression.Parameter(typeof(TInstance));
                if(propertyInfo.PropertyType != typeof(TValue))
                    func = Expression.Lambda<Func<TInstance, TValue>>(Expression.Convert(Expression.Property(instanceParameter, propertyInfo), typeof(TValue)), instanceParameter).Compile();
                else
                    func = Expression.Lambda<Func<TInstance, TValue>>(Expression.Property(instanceParameter, propertyInfo), instanceParameter).Compile();

                _getPropertyFunctions.Add(propertyInfo, func);
            }

            return func(instance);

        }

        static Expression<Func<TInstance, bool>> GetPredicat<TInstance, TValue>(PropertyInfo propertyInfo, TValue equalsToValue)
        {
            ParameterExpression param = Expression.Parameter(typeof(TInstance));
            return Expression.Lambda<Func<TInstance, bool>>(Expression.Equal(Expression.Property(param, propertyInfo), Expression.Constant(equalsToValue, propertyInfo.PropertyType)), param);
        }
        #endregion
        #endregion

        #region IDisposeable Implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UserStore()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _objectProvider = null;
                _options = null;
                _roleStore = null;
                _users = null;
                _usersInRole = null;
            }
        }

        #endregion
    }
}
