using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Repository;
using CovidMassTesting.Repository.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;

namespace CovidMassTesting
{
    /// <summary>
    /// Startup class
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        private IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();


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


            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder(
                    JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build();
            });

            var key = Encoding.ASCII.GetBytes(Configuration["JWTTokenSecret"]);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
              .AddJwtBearer(x =>
              {
                  x.RequireHttpsMetadata = false;
                  x.SaveToken = true;
                  x.TokenValidationParameters = new TokenValidationParameters
                  {
                      ValidateIssuerSigningKey = true,
                      IssuerSigningKey = new SymmetricSecurityKey(key),
                      ValidateIssuer = false,
                      ValidateAudience = false
                  };
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

            var redisConfiguration = new RedisConfiguration();
            try
            {
                Configuration.GetSection("Redis")?.Bind(redisConfiguration);

            }
            catch (Exception exc)
            {
                Console.Error.WriteLine($"{exc.Message} {exc.InnerException?.Message}");
            }
            if (string.IsNullOrEmpty(redisConfiguration?.Hosts?.FirstOrDefault()?.Host))
            {
                services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfiguration);


                services.AddSingleton<IPlaceRepository, Repository.MockRepository.PlaceRepository>();
                services.AddSingleton<ISlotRepository, Repository.MockRepository.SlotRepository>();
                services.AddSingleton<IUserRepository, Repository.MockRepository.UserRepository>();
                services.AddSingleton<IVisitorRepository, Repository.MockRepository.VisitorRepository>();
            }
            else
            {
                services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfiguration);

                services.AddSingleton<IPlaceRepository, Repository.RedisRepository.PlaceRepository>();
                services.AddSingleton<ISlotRepository, Repository.RedisRepository.SlotRepository>();
                services.AddSingleton<IUserRepository, Repository.RedisRepository.UserRepository>();
                services.AddSingleton<IVisitorRepository, Repository.RedisRepository.VisitorRepository>();
            }


            var sendGridConfiguration = Configuration.GetSection("SendGrid")?.Get<Model.Settings.SendGridConfiguration>();
            if (string.IsNullOrEmpty(sendGridConfiguration?.MailerApiKey))
            {
                services.AddSingleton<IEmailSender, Controllers.Email.NoEmailSender>();
            }
            else
            {
                services.AddSingleton<IEmailSender, Controllers.Email.SendGridController>();
            }

        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="userRepository"></param>        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IUserRepository userRepository, ILogger<Startup> logger)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (env is null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            if (userRepository is null)
            {
                throw new ArgumentNullException(nameof(userRepository));
            }

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

            userRepository.CreateAdminUsersFromConfiguration().Wait();
            logger.LogInformation($"App started with db prefix {Configuration["db-prefix"]}");
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
