using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ScarletPigsServices.Api.Repositories;
using ScarletPigsServices.Api.Services.Files;
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

            var authAuthority = builder.Configuration["Authentication:Authority"]
                ?? "https://keycloak.scarletpigs.com/realms/ScarletPigs";
            var authAudience = builder.Configuration["Authentication:Audience"]
                ?? builder.Configuration["Authentication:ClientId"]
                ?? "scarletpigsclient";

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authAuthority;
                    options.Audience = authAudience;
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "global_name",
                        RoleClaimType = ClaimTypes.Role,
                        ValidateAudience = true
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("CanUploadMissions", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireAssertion(context =>
                    {
                        return context.User.Claims.Any(claim =>
                            (claim.Type == ClaimTypes.Role || claim.Type == "roles")
                            && (string.Equals(claim.Value, "UnitOrganizer", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(claim.Value, "MissionMaker", StringComparison.OrdinalIgnoreCase)));
                    });
                });
            });

            // Register services
            builder.AddNpgsqlDbContext<ScarletPigsDbContext>(ServiceRefs.DB);
            builder.Services.AddScoped<IEventRepository, EventRepository>();
            builder.Services.AddSingleton<IHavocFileService, HavocFileService>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Piglet API", Version = "v1" });
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
                options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Provide a Keycloak bearer token to call authenticated endpoints."
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = JwtBearerDefaults.AuthenticationScheme
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
