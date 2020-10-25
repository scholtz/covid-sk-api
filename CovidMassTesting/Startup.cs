using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CovidMassTesting.Repository;
using CovidMassTesting.Repository.Interface;
using CovidMassTesting.Repository.MockRepository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;

namespace CovidMassTesting
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            var redisConfiguration = Configuration.GetSection("Redis").Get<RedisConfiguration>();
            services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfiguration);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Covid Mass Testing API",
                    Version = "v1",
                    Description = "This API has been created for optimisation of time required to take sample from person for Covid-19 test.\n" +
                    "Medical personel scans the bar code on the testing set, scans the bar code of the person registration, and performs test.\n" +
                    "After that the person is free to go home. This application aims to create mass testing possible with minimal risk of contamination from testing personel and minimising the queues in front of testing location."
                });
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First()); //This line

                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Description = "Bearer token.",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey
                });
                c.OperationFilter<Swashbuckle.AspNetCore.Filters.SecurityRequirementsOperationFilter>();

                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"doc/documentation.xml"));
            });

            // Add CORS policy
            var corsConfig = Configuration.GetSection("Cors").AsEnumerable().Select(k => k.Value).Where(k => !string.IsNullOrEmpty(k)).ToArray();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                builder =>
                {
                    builder.WithOrigins(corsConfig)
                                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                                        .AllowAnyMethod()
                                        .AllowAnyHeader()
                                        .AllowCredentials();
                });
            });


            services.AddSingleton<IPlaceRepository, PlaceRepository>();
            services.AddSingleton<ISlotRepository, SlotRepository>();
            services.AddSingleton<IUserRepository, UserRepository>();
            services.AddSingleton<IVisitorRepository, VisitorRepository>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseStaticFiles();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("v1/swagger.json", "Enumerations Service API V1");
            });
            app.UserRedisInformation();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            GC.Collect();
            try
            {
                // For kubernetes readines probe
                File.WriteAllText("ready.txt", DateTimeOffset.Now.ToString("o"));
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
        }
    }
}
