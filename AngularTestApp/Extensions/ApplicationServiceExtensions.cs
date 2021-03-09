using API.Data;
using API.Helpers;
using API.Interfaces;
using API.Services;
using API.Services.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<PresenceTracker>();
            services.Configure<CloudinarySettings>(config.GetSection("CloudinarySettings"));

            services.AddScoped<IPhotoService, PhotoService>();
            services.AddScoped<ITokenService, TokenService>();

            services.AddScoped<LogUserActivity>();
            services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddDbContext<DataContext>(options =>
            {
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                string connStr;

                // Use connection string from file.
                connStr = config.GetConnectionString("DefaultConnection");

                options.UseNpgsql(connStr);
            });

            return services;
        }
    }
}
