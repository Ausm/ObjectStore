using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;
using ObjectStore;
using ObjectStore.Identity;

namespace TestEmpty
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddObjectStore("data source=(local);Integrated Security=True;initial catalog=Test");

            services.AddIdentity<User, Role>()
                .AddObjectStoreUserStores();

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();


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
