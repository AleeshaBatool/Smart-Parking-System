using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MQTTnet.AspNetCore;

using MQTTnet.Server;
using SmartParkingSystem.Controllers;
using SmartParkingSystem.DBContext;
using SmartParkingSystem.Helper;
using System.Net;

namespace SmartParkingSystem
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Dynamically retrieve the local IP address
            string localIp = HelperMethods.GetLocalIpAddress();
            if (string.IsNullOrEmpty(localIp))
            {
                throw new InvalidOperationException("Unable to determine local IP address.");
            }

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Parse(localIp), 1883, listenOptions =>
                {
                    listenOptions.UseMqtt(); // Enables MQTT protocol on this port
                });

                options.Listen(IPAddress.Parse(localIp), 5000); // WebSocket support
            });
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Parse(localIp), 7209); // HTTP port
                options.Listen(IPAddress.Parse(localIp), 7210, listenOptions =>
                {
                    listenOptions.UseHttps(); // HTTPS port
                });
            });

            builder.Services.AddDbContext<AppDbContext>(options =>
           options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Swagger configuration
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Smart Parking System API",
                    Version = "v1",
                    Description = "API documentation for Smart Parking System"
                });
            });

            // MQTT server setup
            builder.Services
                .AddHostedMqttServer(optionsBuilder =>
                {
                    optionsBuilder.WithDefaultEndpoint();
                });

            builder.Services.AddMqttConnectionHandler();
            builder.Services.AddConnections();

            // Optional: Register MQTT-related controller or service
            builder.Services.AddScoped<MqttController>();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

           // app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            using (var scope = app.Services.CreateScope())
            {
                var mqttController = scope.ServiceProvider.GetRequiredService<MqttController>();

                // Map MQTT connection handler
                app.UseEndpoints(endpoints =>
                {
                    // Map MQTT over WebSockets
                    endpoints.MapConnectionHandler<MqttConnectionHandler>("/mqtt", options =>
                    {
                        options.WebSockets.SubProtocolSelector = protocols => protocols.FirstOrDefault() ?? string.Empty;
                    });
                });

                // Configure MQTT server events
                app.UseMqttServer(server =>
                {
                    server.ValidatingConnectionAsync += mqttController.ValidateConnection;
                    server.ClientConnectedAsync += mqttController.OnClientConnected;
                    server.InterceptingPublishAsync += mqttController.HandleIncomingMessage;
                    

                });

            }
           


            app.UseAuthorization();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Parking System API V1");
                c.RoutePrefix = "swagger"; // Accessible at /swagger
            });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await app.RunAsync();
        }
    }
}
