using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Azure_Serverless_SignalR_Core_Emulator.Hubs;
using Azure_Serverless_SignalR_Core_Emulator.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Azure_Serverless_SignalR_Core_Emulator
{
    public class Startup
    {
        private const string SIGNALR_ROUTE = "/client"; 
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MappingService>();
            services.AddControllers();
            services.AddSignalR();
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }
            ).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = false;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateActor = false,
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"])),
                };
                x.Events = new JwtBearerEvents()
                {
                    OnMessageReceived = (context) =>
                    {
                        if (context.Request.Path.StartsWithSegments(new Microsoft.AspNetCore.Http.PathString(SIGNALR_ROUTE)))
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken))
                                context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    // Vaildate the audience here.
                    OnTokenValidated = (context) =>
                    {    
                        string aud = context.Principal.Claims.FirstOrDefault(x => x.Type == "aud")?.Value;
                        if (aud == null)
                            context.Fail("No audience in JWT");
                        if (context.Request.Path.StartsWithSegments(new Microsoft.AspNetCore.Http.PathString(SIGNALR_ROUTE)))
                        {
                            string targetedHub = context.Request.Query["hub"];
                            if (string.IsNullOrEmpty(targetedHub))
                                context.Fail("No hub specified");
                            string audHub = HttpUtility.ParseQueryString(new Uri(aud).Query).Get("hub");
                            if (string.IsNullOrEmpty(audHub) || audHub != targetedHub)
                                context.Fail("Hub mismatch with the one in audience");
                            return Task.CompletedTask;
                        }
                        if (aud != $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}")
                            context.Fail("Audience mismatch with the requested url");
                        return Task.CompletedTask;
                    }
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<SimHub>(SIGNALR_ROUTE);
            });
        }
    }
}
