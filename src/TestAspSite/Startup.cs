using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Identity;
using Microsoft.Framework.DependencyInjection;
using ObjectStore;
using ObjectStore.Identity;
using System;

namespace TestEmpty
{
    public class Startup
    {
        class PasswordHasher : IPasswordHasher<User>
        {
            public string HashPassword(User user, string password)
            {
                return password;
            }

            public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
            {
                return hashedPassword == providedPassword ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
            }
        }

        public Startup()
        {

        }

        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddObjectStore("data source=(local);Integrated Security=True;initial catalog=Test");

            //List<User> users = new List<User>();
            //List<Role> roles = new List<Role>();

            //users.Add(new User("User1", "Password"));
            //roles.Add(new Role("All"));

            //services.Add(new ServiceDescriptor(typeof(IPasswordHasher<User>), typeof(PasswordHasher), ServiceLifetime.Singleton));
            services.AddIdentity<User, Role>()
                .AddObjectStoreUserStores();
            //    .addOb
                //.AddDefaultTokenProviders()
                //.AddCustomUserStores(users.AsQueryable(), roles.AsQueryable(), x => x.Name, x => x.Name, x => x.Password, (u, r) => true);
            //Microsoft.AspNet.Identity.IPasswordHasher
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            //app.UseStaticFiles();


            app.UseIdentity();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
