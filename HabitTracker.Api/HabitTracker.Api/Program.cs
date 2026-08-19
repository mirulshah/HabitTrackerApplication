
using FluentValidation;
using Serilog;
using HabitTracker.Api.Extensions;
using HabitTracker.Application.Features.Auth;
using HabitTracker.Domain.Entities;
using HabitTracker.Infrastructure.Auth;
using HabitTracker.Infrastructure.Persistence;
using HabitTracker.Infrastructure.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Asp.Versioning.ApiExplorer;

namespace HabitTracker.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerWithJwtAuth();

            builder.Services.AddControllers();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            builder.Services.AddJwtAuthentication(builder.Configuration);

            builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

            builder.Host.UseSerilog((context, configuration) =>
            {
                configuration
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs/habittracker-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);
            });

            builder.Services.ExtensionApiVersioning();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

                await DbSeeder.SeedAsync(db, passwordHasher);

                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerEndpoint(
                            $"/swagger/{description.GroupName}/swagger.json",
                            description.GroupName.ToUpperInvariant());
                    }
                });


            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
