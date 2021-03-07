//using API.Extensions;
//using API.Services.SignalR;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.SpaServices.AngularCli;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;

//namespace AngularTestApp
//{
//    public class Startup
//    {
//        public Startup(IConfiguration configuration)
//        {
//            Configuration = configuration;
//        }

//        public IConfiguration Configuration { get; }

//        // This method gets called by the runtime. Use this method to add services to the container.
//        public void ConfigureServices(IServiceCollection services)
//        {
//            services.AddApplicationServices(Configuration);
//            services.AddControllers();
//            services.AddCors();
//            services.AddIdentityServices(Configuration);
//            services.AddSignalR();

//            // In production, the Angular files will be served from this directory
//            services.AddSpaStaticFiles(configuration =>
//            {
//                configuration.RootPath = "wwwroot";
//            });
//        }

//        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
//        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
//        {
//            if (env.IsDevelopment() || true)
//            {
//                app.UseDeveloperExceptionPage();
//            }
//            else
//            {
//                app.UseExceptionHandler("/Error");
//            }

//            app.UseDefaultFiles();
//            app.UseStaticFiles();
//            if (!env.IsDevelopment())
//            {
//                app.UseSpaStaticFiles();
//            }

//            app.UseRouting();
//            app.UseAuthentication();
//            app.UseAuthorization();

//            app.UseEndpoints(endpoints =>
//            {
//                endpoints.MapControllers();
//                endpoints.MapHub<PresenceHub>("hubs/presence");
//                endpoints.MapHub<MessageHub>("hubs/message");
//                endpoints.MapFallbackToController("Index", "Fallback");
//            });

//            app.UseSpa(spa =>
//            {
//                // To learn more about options for serving an Angular SPA from ASP.NET Core,
//                // see https://go.microsoft.com/fwlink/?linkid=864501

//                spa.Options.SourcePath = "ClientApp";

//                if (env.IsDevelopment())
//                {
//                    spa.UseAngularCliServer(npmScript: "start");
//                }
//            });
//        }
//    }
//}

using API.Extensions;
using API.Middleware;
using API.Services.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API
{
    public class Startup
    {
        private readonly IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationServices(_config);
            services.AddControllers();
            services.AddCors();
            services.AddIdentityServices(_config);
            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<ExceptionMiddleware>();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(x => x.AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithOrigins("https://localhost:4200"));

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<PresenceHub>("hubs/presence");
                endpoints.MapHub<MessageHub>("hubs/message");
                endpoints.MapFallbackToController("Index", "Fallback");
            });
        }
    }
}
