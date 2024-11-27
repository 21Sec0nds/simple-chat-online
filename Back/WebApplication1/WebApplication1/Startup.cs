using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebApplication1.Data;
using StackExchange.Redis;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;
using ServerVersion = Microsoft.EntityFrameworkCore.ServerVersion;

namespace dotnet_chat
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            //=======================================================================MongoDB setup=====================================================================
            var mongoClient = new MongoClient("mongodb://alex:alex@10.30.0.2:27017");
            var database = mongoClient.GetDatabase("ChatTable"); // Replace with your actual database name
            services.AddSingleton(database);
            //=======================================================================Redis Setup=====================================================================
            string redisConnection = Configuration.GetConnectionString("Redis");
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
            });
            var redisConfiguration = ConfigurationOptions.Parse("10.30.0.2:9537,password=Alex");
            IConnectionMultiplexer redis = null;
            try
            {
                redis = ConnectionMultiplexer.Connect(redisConfiguration);
                services.AddSingleton(redis);
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine($"Redis Connection Error: {ex.Message}");
            }
            //=======================================================================MySQL DbContext Setup=====================================================================
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                string connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });
            //=======================================================================Adding JWT Authentication=====================================================================
            services.AddAuthentication("Bearer")
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidAudience = Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                    };
                });
            //=======================================================================Adding other services=====================================================================
            services.AddCors();
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

           
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors(options => options
                .WithOrigins(
                    new string[]
                    {
                        "http://localhost:3000", "http://localhost:8080", "http://localhost:4200",
                        "http://localhost:5000"
                    })
                .AllowAnyHeader()
                .AllowAnyMethod()
            );

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
