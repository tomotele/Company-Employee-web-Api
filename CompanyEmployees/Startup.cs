using Entities;
using CompanyEmployees.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NLog;
using System.IO;
using Contracts;
using Presentation.ActionFilters;
using Microsoft.Extensions.DependencyInjection;
using Entities.DataTransferObjects;
using Service.DataShaping;
using CompanyEmployees.Utility;
using Microsoft.AspNetCore.Mvc.Formatters;
using AspNetCoreRateLimit;

namespace CompanyEmployees
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            
            LogManager.LoadConfiguration(string.Concat(Directory.GetCurrentDirectory(),
            "/nlog.config"));

            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            //DATABASE CONNECTION
            services.AddDbContext<RepositoryContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
            );

            services.ConfigureCors();
            services.ConfigureIISIntegration();
            //REPO PATTERN
            services.ConfigureRepositoryManager();
            services.ConfigureServiceManager();
            //LOGGER
            services.ConfigureLoggerService();
            //CACHING
            services.ConfigureResponseCaching();
            services.ConfigureHttpCacheHeaders();
            //RATE LIMITING
            services.AddMemoryCache();
            services.ConfigureRateLimitingOptions();
            services.AddHttpContextAccessor();
            //IDENTITY
            services.AddAuthentication();
            services.ConfigureIdentity();

            //JWT
            services.ConfigureJWT(Configuration);
            services.AddJwtConfiguration(Configuration);

            //AUTHENTICATION HANDLER
            //services.ConfigureAuthenticationHandler();

            //ACTION FILTER
            services.AddScoped<ValidationFilterAttribute>();
            services.AddScoped<ValidateMediaTypeAttribute>();
            //DATA SHAPER
            services.AddScoped<IDataShaper<EmployeeDto>, DataShaper<EmployeeDto>>();
            services.AddScoped<IEmployeeLinks, EmployeeLinks>();
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });
            services.AddAutoMapper(typeof(Startup));
            services.AddControllersWithViews();

            services.AddControllers(config =>
            {
                config.RespectBrowserAcceptHeader = true;
                config.ReturnHttpNotAcceptable = true;
                config.CacheProfiles.Add("120SecondsDuration", new CacheProfile
                {
                    Duration = 120
                });
            })
            .AddNewtonsoftJson()
            .AddApplicationPart(typeof(Presentation.AssemblyReference).Assembly)
            .AddXmlDataContractSerializerFormatters()
            .AddCustomCSVFormatter();
            services.AddCustomMediaTypes();
            services.ConfigureSwagger();
        }

            public void Configure(IApplicationBuilder app, IWebHostEnvironment env, RepositoryContext dbContext, ILoggerManager logger)
        {
            if (env.IsDevelopment())
            {
                dbContext.Database.Migrate();
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CompanyEmployees v1"));
            }
            else
            {
                app.UseHsts();
            }
            //            Microsoft advises that the order of adding different middlewares to the application builder is very important. So the UseRouting() method should be called before the UseAuthorization() method and UseCors() or UseStaticFiles() have to be called before the UseRouting() method.
            app.ConfigureExceptionHandler(logger);
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseIpRateLimiting();
            app.UseCors("CorsPolicy");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseResponseCaching();
            app.UseHttpCacheHeaders();

            app.UseForwardedHeaders(new ForwardedHeadersOptions //will forward proxy headers to the current request.This will help us during application deployment.

            {
                ForwardedHeaders = ForwardedHeaders.All
            });
            

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

           
        }

    }
}
