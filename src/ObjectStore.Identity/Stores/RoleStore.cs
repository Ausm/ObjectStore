using System;
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
    public class RoleStore<TRole> : IQueryableRoleStore<TRole> where TRole : class
    {
        #region Fields
        static Dictionary<PropertyInfo, Delegate> _setPropertyFunctions;
        static Dictionary<PropertyInfo, Delegate> _getPropertyFunctions;

        IObjectProvider _objectProvider;
        IQueryable<TRole> _roles;
        UserStoreOptions _options;
        #endregion

        #region Constructors
        static RoleStore()
        {
            _setPropertyFunctions = new Dictionary<PropertyInfo, Delegate>();
            _getPropertyFunctions = new Dictionary<PropertyInfo, Delegate>();
        }
        public RoleStore(IObjectProvider objectProvider, IOptions<UserStoreOptions> options)
        {
            _objectProvider = objectProvider;
            _roles = objectProvider.GetQueryable<TRole>();
            _options = options.Value;
        }
        #endregion

        #region Properties
        public IQueryable<TRole> Roles
        {
            get
            {
                return _roles;
            }
        }
        #endregion

        #region Methods
        public Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken)
        {
            TRole newRole = _objectProvider.CreateObject<TRole>();
            SetProperty(newRole, _options.RoleNameProperty, GetProperty<TRole, string>(role, _options.RoleNameProperty));
            SetProperty(newRole, _options.NormalizedRolenameProperty, GetProperty<TRole, string>(role, _options.NormalizedRolenameProperty));
            _roles.Where(x => x == newRole).Save();
            return Task.FromResult(IdentityResult.Success);
        }
        public Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken)
        {
            IQueryable<TRole> roles = _roles.Where(x => x == role);
            roles.Delete();
            roles.Save();
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(GetProperty<TRole, string>(role, _options.NormalizedRolenameProperty));
        }

        public Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken)
        {
            SetProperty(role, _options.NormalizedRolenameProperty, normalizedName);
            return Task.FromResult(true);
        }

        public Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(_options.ConvertIdToString(GetProperty<TRole, object>(role, _options.RoleIdProperty)));
        }

        public Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(GetProperty<TRole, string>(role, _options.RoleNameProperty));
        }

        public Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken)
        {
            SetProperty(role, _options.RoleNameProperty, roleName);
            return Task.FromResult(true);
        }

        public Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken)
        {
            _roles.Where(x => x == role).Save();
            return Task.FromResult(IdentityResult.Success);
        }

        public async Task<TRole> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            object convertedId = _options.ConvertStringToId(roleId);

            IQueryable<TRole> roles = _roles.Where(GetPredicat<TRole, object>(_options.RoleIdProperty, convertedId));
            await roles.FetchAsync();

            return roles.FirstOrDefault();
        }

        public async Task<TRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            IQueryable<TRole> roles = _roles.Where(GetPredicat<TRole, string>(_options.NormalizedRolenameProperty, normalizedRoleName));
            await roles.FetchAsync();

            return roles.FirstOrDefault();
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

        ~RoleStore()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _objectProvider = null;
                _roles = null;
                _options = null;
            }
        }

        #endregion
    }
}
