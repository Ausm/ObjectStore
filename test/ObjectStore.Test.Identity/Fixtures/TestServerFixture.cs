using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ObjectStore.Sqlite;
using System;
using System.Net.Http;
using ObjectStore.Identity;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Xunit;
using ObjectStore.Interfaces;
using ObjectStore.OrMapping;
using System.IO;
using System.Reflection;
using E = ObjectStore.Test.Identity.Entities;

namespace ObjectStore.Test.Identity.Fixtures
{
    public class TestServerFixture : IDisposable
    {
        TestServer _server;
        HttpClient _client;
        RequestDelegate _function;
        string _databaseFileName;

        public TestServerFixture()
        {
            {
                string directory = Path.GetDirectoryName(typeof(TestServerFixture).GetTypeInfo().Assembly.Location);
                int i = 0;
                _databaseFileName = $"Database{i}.db";
                while (File.Exists(Path.Combine(directory, _databaseFileName)))
                {
                    try
                    {
                        File.Delete(Path.Combine(directory, _databaseFileName));
                        break;
                    }
                    catch
                    {
                        _databaseFileName = $"Database{++i}.db";
                    }
                }
            }
                

            _server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddObjectStoreWithSqlite($"Data Source={_databaseFileName}");
                    services.AddIdentity<E.User, E.Role>()
                        .AddObjectStoreUserStores<E.User, E.Role, E.UserInRole>();
                })
                .Configure(app =>
                {
                    app.UseIdentity().Use(d => c => _function == null ? d(c) : _function(c));
                    RelationalObjectStore objectStore = app.ApplicationServices.GetService(typeof(IObjectProvider)) as RelationalObjectStore;
                    objectStore.Register<E.User>().Register<E.Role>().Register<E.UserInRole>();

                    objectStore.InitializeDatabase(databaseInitializer => {
                        databaseInitializer.RegisterTableStatement(x => x.Tablename == "`dbo.Roles`", (x, s) => s + ";INSERT INTO `dbo.Roles` (`Name`, `NormalizedRolename`) VALUES ('Admin', 'ADMIN'), ('Test', 'TEST')");
                        databaseInitializer.RegisterTableStatement(x => x.Tablename == "`dbo.Users`", (x, s) => s + ";INSERT INTO `dbo.Users` (`Name`, `Password`, `NormalizedUsername`) VALUES ('Admin', 'AQAAAAEAACcQAAAAEH7ZcGTOm+i5+wDjYcKunrChCybl3/XfsGoRnchwXssH9swyQYCLETt+H39cXjacaA==', 'ADMIN'), ('User1', 'AQAAAAEAACcQAAAAEH3QfNo5cHGK4myXlU1XHm6I1t+e02CPXhdAqlBiE4h+7VcJdwkI0u2A3uNC0TgGkQ==', 'USER1')");
                        databaseInitializer.RegisterTableStatement(x => x.Tablename == "`dbo.UsersInRole`", (x, s) => "CREATE TABLE `dbo.UsersInRole` ( `User` INTEGER NOT NULL, `Role` INTEGER NOT NULL, PRIMARY KEY(User,Role), FOREIGN KEY(`User`) REFERENCES `dbo.Users`(`Id`), FOREIGN KEY(`Role`) REFERENCES `dbo.Roles`(`Id`) );INSERT INTO `dbo.UsersInRole` (`User`, `Role`) VALUES (1, 1)");
                    });
                }));
            _client = _server.CreateClient();
        }

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
