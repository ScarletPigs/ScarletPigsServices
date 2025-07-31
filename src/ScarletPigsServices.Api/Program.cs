using Microsoft.OpenApi.Models;
using ScarletPigsServices.Api.Repositories;
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

            // Register services
            builder.AddNpgsqlDbContext<ScarletPigsDbContext>(ServiceRefs.DB);
            builder.Services.AddScoped<IEventRepository, EventRepository>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Piglet API", Version = "v1" });
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
            });


            var app = builder.Build();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
