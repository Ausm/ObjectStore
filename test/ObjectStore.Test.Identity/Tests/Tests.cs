using Xunit;
using System;
using System.Linq;
using Xunit.Abstractions;
using System.Threading.Tasks;
using ObjectStore.Test.Identity.Fixtures;
using Microsoft.AspNetCore.Identity;
using ObjectStore.Identity;
using ObjectStore.Interfaces;

namespace ObjectStore.Test.Identity
{
    public class IdentityTests : IClassFixture<TestServerFixture>
    {
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
        public async Task TestRegister()
        {
            IdentityResult identityResult = await _fixture.Execute(async (UserManager<User> userManager, IObjectProvider objectProvider) => {
                User user = objectProvider.CreateObject<User>();
                user.Name = $"Test{(DateTime.Now - new DateTime(2016, 1, 1)).TotalHours}";
                user.Password = string.Empty;
                user.NormalizedUsername = user.Name;
                objectProvider.GetQueryable<User>().Where(x => x == user).Save();
                return await userManager.CreateAsync(user, "Passw0rd!");
            });

            Assert.True(identityResult.Succeeded);
        }
        #endregion
    }
}
