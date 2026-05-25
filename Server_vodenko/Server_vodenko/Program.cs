using Server_vodenko.Application.Interfaces;
using Server_vodenko.Application.Services;
using Server_vodenko.Domain;
using Server_vodenko.Infrastructure.Repository;
using Server_vodenko.Infrastructure.BackgroundServices;

namespace Server_vodenko
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton<PlcDataCache>();
            builder.Services.AddSingleton<PlcConnection>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<PlcConnection>());

            builder.Services.AddScoped<IVodenkoRepository, VodenkoRepository>();
            builder.Services.AddScoped<IVodenkoService, VodenkoService>();


            //Dependency injection (DI)
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapControllers();
            app.Run();
        }
    }
}
