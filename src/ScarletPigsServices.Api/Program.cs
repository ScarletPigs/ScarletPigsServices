using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using ScarletPigsServices.Api.Authentication;
using ScarletPigsServices.Api.Repositories;
using ScarletPigsServices.Api.Services.Files;
using ScarletPigsServices.Api.Services.Workshop;
using ScarletPigsServices.Data;
using ScarletPigsServices.ServiceReferences;
using System.Reflection;

namespace ScarletPigsServices.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.AddServiceDefaults();

            builder.Services.AddControllers();

            builder.Services
                .AddOptions<ApiKeyAuthenticationOptions>(ApiKeyAuthenticationDefaults.AuthenticationScheme)
                .Bind(builder.Configuration.GetSection(ApiKeyAuthenticationOptions.SectionName))
                .Validate(options => !string.IsNullOrWhiteSpace(options.Key), "ApiKey:Key is required.")
                .Validate(
                    options => Encoding.UTF8.GetByteCount(options.Key) >= 32,
                    "ApiKey:Key must contain at least 32 bytes.")
                .ValidateOnStart();

            builder.Services
                .AddAuthentication(ApiKeyAuthenticationDefaults.AuthenticationScheme)
                .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                    ApiKeyAuthenticationDefaults.AuthenticationScheme,
                    static _ => { });

            builder.Services.AddAuthorization(options =>
            {
                var apiKeyPolicy = new AuthorizationPolicyBuilder(ApiKeyAuthenticationDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();

                options.DefaultPolicy = apiKeyPolicy;
                options.FallbackPolicy = apiKeyPolicy;
            });

            // Register services
            builder.AddNpgsqlDbContext<ScarletPigsDbContext>(ServiceRefs.DB);
            builder.Services.AddScoped<IEventRepository, EventRepository>();
            builder.Services.AddSingleton<IHavocFileService, HavocFileService>();
            builder.Services.AddHttpClient<ISteamWorkshopService, SteamWorkshopService>(client =>
            {
                client.BaseAddress = new Uri("https://api.steampowered.com/");
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Piglet API", Version = "v1" });
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
                options.AddSecurityDefinition(ApiKeyAuthenticationDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Name = ApiKeyAuthenticationDefaults.HeaderName,
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description = $"Provide the configured API key in the {ApiKeyAuthenticationDefaults.HeaderName} header."
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = ApiKeyAuthenticationDefaults.AuthenticationScheme
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });


            var app = builder.Build();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
