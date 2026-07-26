using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ScarletPigsServices.Api.Authentication;
using ScarletPigsServices.Api.Repositories;
using ScarletPigsServices.Api.Services.Files;
using ScarletPigsServices.Api.Services.Workshop;
using ScarletPigsServices.Data;
using ScarletPigsServices.Data.Auth;
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

            builder.Services.AddOptions<JwtOptions>()
                .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
                .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Authentication:Issuer is required.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Authentication:Audience is required.")
                .Validate(
                    options => Encoding.UTF8.GetByteCount(options.SigningKey) >= 32,
                    "Authentication:SigningKey must contain at least 32 bytes.")
                .Validate(options => options.AccessTokenMinutes > 0, "Authentication:AccessTokenMinutes must be greater than zero.")
                .Validate(options => options.RefreshTokenDays > 0, "Authentication:RefreshTokenDays must be greater than zero.")
                .ValidateOnStart();

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                });

            builder.Services
                .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
                {
                    var jwtOptions = jwtOptionsAccessor.Value;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,
                        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                        ClockSkew = TimeSpan.FromSeconds(30),
                        NameClaimType = ClaimTypes.Name,
                        RoleClaimType = ClaimTypes.Role,
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("CanUploadMissions", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole(AuthRoles.UnitOrganizer, AuthRoles.MissionMaker);
                });
            });

            // Register services
            builder.AddNpgsqlDbContext<ScarletPigsDbContext>(ServiceRefs.DB);
            builder.Services
                .AddIdentityCore<ApplicationUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 12;
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddRoles<IdentityRole>()
                .AddSignInManager()
                .AddEntityFrameworkStores<ScarletPigsDbContext>()
                .AddDefaultTokenProviders();
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddScoped<ITokenService, JwtTokenService>();
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
                options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Provide an access token issued by the Scarlet Pigs authentication endpoints."
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
