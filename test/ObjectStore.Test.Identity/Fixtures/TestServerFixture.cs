using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ObjectStore.Sqlite;
using System;
using System.Net.Http;
using ObjectStore.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Xunit;

namespace ObjectStore.Test.Identity.Fixtures
{
    public class TestServerFixture : IDisposable
    {
        TestServer _server;
        HttpClient _client;
        RequestDelegate _function;

        public TestServerFixture()
        {
            _server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddObjectStoreWithSqlite("Data Source=Database/Database.db");
                    services.AddIdentity<User, Role>()
                        .AddObjectStoreUserStores();
                })
                .Configure(app => app.UseIdentity().Use(d => c => _function == null ? d(c) : _function(c))));
            _client = _server.CreateClient();
        }

        public SignInManager<User> SignInManager => _server.Host.Services.GetService(typeof(SignInManager<User>)) as SignInManager<User>;

        public async Task<T> Execute<T>(Func<HttpContext, Task<T>> func)
        {
            try
            {
                T returnValue = default(T);
                bool isCalled = false;

                _function = async c => { returnValue = await func(c); isCalled = true; };

                await _client.GetAsync("/");

                Assert.True(isCalled, "Http call didn't call the given function.");
                return returnValue;
            }
            finally
            {
                _function = null;
            }
        }

        public async Task<TResult> Execute<T1, TResult>(Func<HttpContext, T1, Task<TResult>> func)
        {
            return await Execute(async c =>
            {
                T1 service1 = (T1)c.RequestServices.GetService(typeof(T1));
                return await func(c, service1);
            });
        }
        public async Task<TResult> Execute<T1, TResult>(Func<T1, Task<TResult>> func)
        {
            return await Execute(async c =>
            {
                T1 service1 = (T1)c.RequestServices.GetService(typeof(T1));
                return await func(service1);
            });
        }

        public async Task<TResult> Execute<T1, T2, TResult>(Func<T1, T2, Task<TResult>> func)
        {
            return await Execute(async c =>
            {
                T1 service1 = (T1)c.RequestServices.GetService(typeof(T1));
                T2 service2 = (T2)c.RequestServices.GetService(typeof(T2));
                return await func(service1, service2);
            });
        }

        public void Dispose()
        {
            _server.Dispose();
            _client.Dispose();
            _server = null;
            _client = null;
        }
    }
}
