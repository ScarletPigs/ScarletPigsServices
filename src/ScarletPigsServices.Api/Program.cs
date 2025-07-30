
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Piglet_API.Data;
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

            // Run database migrations
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<PigletDBContext>();
                context.Database.Migrate();
            }

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
