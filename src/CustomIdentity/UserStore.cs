using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNet.Identity;

namespace CustomIdentity
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
        Func<TUser, TUserKey> _getUserKey;
        Func<TRole, TRoleKey> _getRoleKey;
        Func<TUser, string> _getUserId;
        Func<TRole, string> _getRoleId;
        Func<TRole, string> _getRoleName;
        Func<TUser, string> _getUserName;
        Func<TUser, string> _getUserPasswordHash;
        Func<TUser, TRole, bool> _getIsUserInRole;
        #endregion

        #region Constructors
        public UserStore(IQueryable<TUser> users, IQueryable<TRole> roles, 
            Func<TUser, TUserKey> getUserKey, 
            Func<TUser, string> getUserId,
            Func<TUser, string> getUserName,
            Func<TUser, string> getUserPasswordHash,
            Func<TRole, TRoleKey> getRoleKey,
            Func<TRole, string> getRoleId,
            Func<TRole, string> getRoleName,
            Func<TUser, TRole, bool> getIsUserInRole
            )
        {
            _users = users;
            _roles = roles;
            _getRoleKey = getRoleKey;
            _getUserKey = getUserKey;
            _getUserId = getUserId;
            _getRoleId = getRoleId;
            _getUserPasswordHash = getUserPasswordHash;
            _getIsUserInRole = getIsUserInRole;
            _getUserName = getUserName;
            _getRoleName = getRoleName;
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
            throw new NotSupportedException();
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
            throw new NotSupportedException();
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region Implementations
        public Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(_getRoleId(role));
        }

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(_getUserId(user));
        }

        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(_getUserPasswordHash(user));
        }

        public Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(_getRoleId(role));
        }

        public Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(_getRoleName(role));
        }

        public Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult<IList<string>>(_roles.Where(x => _getIsUserInRole(user, x)).Select(x => _getRoleName(x)).ToList());
        }

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(_getUserId(user));
        }

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(_getUserName(user));
        }

        public Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            TRole role = _roles.Where(x => _getRoleName(x) == roleName).FirstOrDefault();

            return Task.FromResult<IList<TUser>>(role == null ? new List<TUser>() : _users.Where(x => _getIsUserInRole(x, role)).ToList());
        }

        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            TRole role = _roles.Where(x => _getRoleName(x) == roleName).FirstOrDefault();
            return Task.FromResult(role == null ? false : _getIsUserInRole(user, role));
        }

        Task<TUser> IUserStore<TUser>.FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.Where(x => _getUserId(x) == userId).FirstOrDefault());
        }

        Task<TUser> IUserStore<TUser>.FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.Where(x => _getUserName(x).ToUpper() == normalizedUserName.ToUpper()).FirstOrDefault());
        }

        Task<TRole> IRoleStore<TRole>.FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_roles.Where(x => _getRoleId(x) == roleId).FirstOrDefault());
        }

        Task<TRole> IRoleStore<TRole>.FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            return Task.FromResult(_roles.Where(x => _getRoleName(x) == normalizedRoleName).FirstOrDefault());
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
