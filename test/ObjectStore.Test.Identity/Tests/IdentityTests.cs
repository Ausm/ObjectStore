using Xunit;
using System;
using System.Linq;
using Xunit.Abstractions;
using System.Threading.Tasks;
using ObjectStore.Test.Identity.Fixtures;
using Microsoft.AspNetCore.Identity;
using ObjectStore.Identity;
using ObjectStore.Interfaces;
using System.Collections.Generic;

namespace ObjectStore.Test.Identity
{
    public class IdentityTests : IClassFixture<TestServerFixture>
    {
        #region Subclasses
        class UserMock : User
        {
            public override int Id => 0;

            public override string Name { get; set; }

            public override string NormalizedUsername { get; set; }

            public override string Password { get; set; }
        }

        class RoleMock : Role
        {
            public override int Id => 0;

            public override string Name { get; set; }

            public override string NormalizedRolename { get; set; }
        }
        #endregion

        #region Fields
        ITestOutputHelper _output;
        TestServerFixture _fixture;
        #endregion

        #region Constructor
        public IdentityTests(ITestOutputHelper output, TestServerFixture fixture)
        {
            _output = output;
            _fixture = fixture;
        }
        #endregion

        #region Tests
        [Fact]
        public async Task TestSignInSuccess()
        {
            SignInResult result = await _fixture.Execute((SignInManager<User> signInManager) => signInManager.PasswordSignInAsync("Admin", "test", false, false));

            Assert.NotNull(result);
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task TestSignInFail()
        {
            SignInResult result = await _fixture.Execute((SignInManager<User> signInManager) => signInManager.PasswordSignInAsync("Admin", "1234", false, false));

            Assert.NotNull(result);
            Assert.False(result.Succeeded);
        }

        [Fact()]
        public async Task TestRegisterAndDeleteUser()
        {
            string userName = $"Test{(DateTime.Now - new DateTime(2016, 1, 1)).TotalHours}";

            IdentityResult identityResult = await _fixture.Execute(async (UserManager<User> userManager) => await userManager.CreateAsync(new UserMock() { Name = userName }, "Passw0rd!"));

            Assert.True(identityResult.Succeeded);

            await ResetLocalChanges();

            SignInResult signInResult = await _fixture.Execute((SignInManager<User> signInManager) => signInManager.PasswordSignInAsync(userName, "Passw0rd!", false, false));

            Assert.True(signInResult.Succeeded);

            await ResetLocalChanges();

            identityResult = await _fixture.Execute(async (UserManager<User> userManager) => await userManager.DeleteAsync(await userManager.FindByNameAsync(userName)));

            Assert.True(signInResult.Succeeded);
        }

        [Fact()]
        public async Task TestChangePassword()
        {
            IdentityResult identityResult = await _fixture.Execute(async (UserManager<User> userManager, IObjectProvider objectProvider) => {
                User user = objectProvider.GetQueryable<User>().Where(x => x.Name == "User1").FirstOrDefault();
                return await userManager.ChangePasswordAsync(user, "Passw0rd!", "testPassword1!");
            });

            Assert.True(identityResult.Succeeded);

            await ResetLocalChanges();

            SignInResult signInResult = await _fixture.Execute((SignInManager<User> signInManager) => signInManager.PasswordSignInAsync("User1", "testPassword1!", false, false));

            Assert.True(signInResult.Succeeded);
        }

        [Fact()]
        public async Task TestUserInRole()
        {
            IList<User> result = await _fixture.Execute(async (UserManager<User> userManager) => await userManager.GetUsersInRoleAsync("Admin"));

            Assert.Collection(result,
                x => Assert.Equal("Admin", x.Name));
        }

        [Fact()]
        public async Task TestAddAndRemoveUserFromRole()
        {
            IdentityResult addToRoleResult = await _fixture.Execute(async (UserManager<User> userManager) =>
            {
                User user = await userManager.FindByIdAsync("2");
                return await userManager.AddToRoleAsync(user, "Test");
            });

            Assert.True(addToRoleResult.Succeeded);

            await ResetLocalChanges();

            IList<User> users = await _fixture.Execute(async (UserManager<User> userManager) => await userManager.GetUsersInRoleAsync("Test"));

            Assert.True(users.Any(x => x.Id == 2));

            IdentityResult removeFromRoleResult = await _fixture.Execute(async (UserManager<User> userManager) =>
            {
                User user = await userManager.FindByIdAsync("2");
                return await userManager.RemoveFromRoleAsync(user, "Test");
            });

            Assert.True(removeFromRoleResult.Succeeded);

            await ResetLocalChanges();

            users = await _fixture.Execute(async (UserManager<User> userManager) => await userManager.GetUsersInRoleAsync("Test"));

            Assert.False(users.Any(x => x.Id == 2));

        }

        [Fact()]
        public async Task TestCreateAndRemoveRole()
        {
            IdentityResult addRoleResult = await _fixture.Execute(async (RoleManager<Role> roleManager) => await roleManager.CreateAsync(new RoleMock() { Name = "NewRole" }));

            Assert.True(addRoleResult.Succeeded);

            await ResetLocalChanges();

            IdentityResult deleteRoleResult = await _fixture.Execute(async (RoleManager<Role> roleManager) => await roleManager.DeleteAsync(await roleManager.FindByNameAsync("NewRole")));

            Assert.True(deleteRoleResult.Succeeded);
        }
        #endregion

        #region Methods
        static async Task ResetLocalChanges(IObjectProvider objectProvider = null)
        {
            if (objectProvider == null)
                objectProvider = ObjectStoreManager.DefaultObjectStore;

            objectProvider.GetQueryable<User>().DropChanges();
            objectProvider.GetQueryable<Role>().DropChanges();
            objectProvider.GetQueryable<UserInRole<User, Role>>().DropChanges();
            await objectProvider.GetQueryable<User>().FetchAsync();
            await objectProvider.GetQueryable<Role>().FetchAsync();
            await objectProvider.GetQueryable<UserInRole<User, Role>>().FetchAsync();
        }
        #endregion
    }
}
