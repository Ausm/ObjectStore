using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Identity;
using ObjectStore.Interfaces;
using Microsoft.Extensions.Options;

namespace ObjectStore.Identity
{
    public class UserStore<TUser, TRole, TUserKey, TRoleKey> :
        IUserRoleStore<TUser>,
        IUserPasswordStore<TUser>,
        IQueryableRoleStore<TRole>,
        IQueryableUserStore<TUser>
        where TUser : class
        where TRole : class
        where TUserKey : IEquatable<TUserKey>
        where TRoleKey : IEquatable<TRoleKey>
    {

        #region Fields
        IQueryable<TUser> _users;
        IQueryable<TRole> _roles;
        UserStoreOptions<TUser, TRole, TUserKey, TRoleKey> _options;

        #endregion

        #region Constructors
        public UserStore(IObjectProvider objectProvider, IOptions<UserStoreOptions<TUser, TRole, TUserKey, TRoleKey>> options)
        {
            _users = objectProvider.GetQueryable<TUser>();
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

        public IQueryable<TUser> Users
        {
            get
            {
                return _users;
            }
        }
        #endregion

        #region Methods
        #region NotSupported
        public Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
        public Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
        {
            _users.Where(x => x == user).Save();
            return Task.FromResult(IdentityResult.Success);
        }
        public Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
        public Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken)
        {
            if (_options.SetUserNormalizedUsername == null)
                throw new NotSupportedException();

            _options.SetUserNormalizedUsername(user, normalizedName);
            return Task.FromResult(false);
        }
        #endregion

        #region Implementations


        public Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(_options.GetRoleId(role));
        }

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(_options.GetUserNormalizedUsername(user));
        }

        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(_options.GetUserPasswordHash(user));
        }

        public Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(_options.GetRoleId(role));
        }

        public Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(_options.GetRoleName(role));
        }

        public Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult<IList<string>>(_roles.ToList().Where(x => _options.GetIsUserInRole(user, x)).Select(x => _options.GetRoleName(x)).ToList());
        }

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(_options.GetUserId(user));
        }

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(_options.GetUserName(user));
        }

        public Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            TRole role = _roles.Where(x => _options.GetRoleName(x) == roleName).FirstOrDefault();

            return Task.FromResult<IList<TUser>>(role == null ? new List<TUser>() : _users.Where(x => _options.GetIsUserInRole(x, role)).ToList());
        }

        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            TRole role = _roles.Where(x => _options.GetRoleName(x) == roleName).FirstOrDefault();
            return Task.FromResult(role == null ? false : _options.GetIsUserInRole(user, role));
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken)
        {
            if(_options.SetUserPasswordHash == null)
                throw new NotSupportedException();

            _options.SetUserPasswordHash(user, passwordHash);
            return Task.FromResult(false);
        }

        public Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken)
        {
            if(_options.SetRoleName == null)
                throw new NotSupportedException();

            _options.SetRoleName(role, roleName);
            return Task.FromResult(false);
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken)
        {
            if(_options.SetUserName == null)
                throw new NotSupportedException();

            _options.SetUserName(user, userName);
            return Task.FromResult(false);
        }

        public Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken)
        {
            _roles.Where(x => x == role).Save();
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
        {
            _users.Where(x => x == user).Save();
            return Task.FromResult(IdentityResult.Success);
        }

        Task<TUser> IUserStore<TUser>.FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.ToList().Where(x => _options.GetUserId(x) == userId).FirstOrDefault());
        }

        Task<TUser> IUserStore<TUser>.FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.ToList().Where(x => _options.GetUserName(x).ToUpper() == normalizedUserName.ToUpper()).FirstOrDefault());
        }

        Task<TRole> IRoleStore<TRole>.FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_roles.Where(x => _options.GetRoleId(x) == roleId).FirstOrDefault());
        }

        Task<TRole> IRoleStore<TRole>.FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            return Task.FromResult(_roles.Where(x => _options.GetRoleName(x) == normalizedRoleName).FirstOrDefault());
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
                //_users = null;
                //_roles = null;
                //_getRoleKey = null;
                //_getUserKey = null;
            }
        }

        #endregion
    }
}
