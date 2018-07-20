using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppBuilder {
    public class Startup {
        public Startup(IHostingEnvironment env) {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true);

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services) {
            services.Configure<BuildOptions>(Configuration.GetSection("BuildOptions"));
            services.AddHostedService<BuildHostedService4Android>();
//            services.AddSingleton<IBuildQueue, BuildQueue>();

            services.AddDbContext<BuildTaskDbContext>(options => options.UseSqlite("Data Source=task.db"));
            services.AddTransient<IBuildTaskDataService, BuildTaskDataService>();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseMvc(routes => { routes.MapRoute("default", "{controller=App}/{action=Index}/{user?}/{host?}"); });
        }
    }
}