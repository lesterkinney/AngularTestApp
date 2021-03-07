using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace API.Middleware
{


    public class ExceptionMiddleware
    {
        private RequestDelegate Next { get; }
        private ILogger<ExceptionMiddleware> Logger { get; }
        private IHostEnvironment Env { get; }

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            Next = next;
            Logger = logger;
            Env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await this.Next(context);
            }
            catch(Exception ex)
            {
                this.Logger.LogError(ex, ex.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var response = this.Env.IsDevelopment() 
                    ? new ApiException(context.Response.StatusCode, ex.Message, ex.StackTrace?.ToString()) 
                    : new ApiException(context.Response.StatusCode, "Internal Server Error");
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(response, options);
                await context.Response.WriteAsync(json);
            }
        }
    }
}