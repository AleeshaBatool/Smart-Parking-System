using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;
using SmartParkingSystem.DBContext;
using SmartParkingSystem.Helper;
using SmartParkingSystem.Models;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SmartParkingSystem.Controllers
{
    public class MqttController
    {
        
        private readonly IServiceScopeFactory _scopeFactory;


        public MqttController(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // Define valid users
        private readonly Dictionary<string, string> _validUsers = new()
        {
            { "admin", "admin123" },
            { "user1", "pass1" }
        };

        public Task ValidateConnection(ValidatingConnectionEventArgs eventArgs)
        {
            Console.WriteLine($"Client '{eventArgs.ClientId}' is attempting to connect.");

            if (string.IsNullOrWhiteSpace(eventArgs.UserName) || string.IsNullOrWhiteSpace(eventArgs.Password))
            {
                eventArgs.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                Console.WriteLine("Connection rejected: Missing username or password.");
                return Task.CompletedTask;
            }

            if (!_validUsers.TryGetValue(eventArgs.UserName, out var expectedPassword) || eventArgs.Password != expectedPassword)
            {
                eventArgs.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                Console.WriteLine($"Connection rejected: Invalid credentials for user '{eventArgs.UserName}'.");
                return Task.CompletedTask;
            }

            eventArgs.ReasonCode = MqttConnectReasonCode.Success;
            Console.WriteLine($"Client '{eventArgs.ClientId}' connected successfully.");
            return Task.CompletedTask;
        }

        public Task OnClientConnected(ClientConnectedEventArgs eventArgs)
        {
            Console.WriteLine($"Client '{eventArgs.ClientId}' has connected.");
            return Task.CompletedTask;
        }

        public async Task HandleIncomingMessage(InterceptingPublishEventArgs args)
        {
            var topic = args.ApplicationMessage.Topic;
            var payloadSequence = args.ApplicationMessage.Payload;

            if (payloadSequence.IsEmpty)
            {
                Console.WriteLine("Received empty payload.");
                return;
            }

            string payload = Encoding.UTF8.GetString(payloadSequence);

            if (string.IsNullOrEmpty(payload))
            {
                Console.WriteLine("Received empty payload.");
                return;
            }

            if (topic.Equals("Sensors", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var message = JsonConvert.DeserializeObject<ParkingStatus>(payload);
                    Console.WriteLine($"Received parking status update:");
                    Console.WriteLine($"Parking Name: {message.parkingName}");
                    Console.WriteLine($"Status: {message.status}");
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var parking = await _context.Parkings.FirstOrDefaultAsync(p => p.name == message.parkingName);
                        if (parking != null)
                        {
                            parking.isOccupied = message.status;
                            await _context.SaveChangesAsync();
                            Console.WriteLine("Parking status updated successfully.");
                        }
                        else
                        {
                            Console.WriteLine("Parking name not found in database.");
                        }
                    }

                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Failed to parse JSON: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating parking status: {ex.Message}");
                }
            }



            if (topic.Equals("CarEntry", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var message = JsonConvert.DeserializeObject<VehicleParkingMQTT>(payload);
                    if (message == null || string.IsNullOrEmpty(message.vehicleNo) || string.IsNullOrEmpty(message.vehicleType))
                        return;

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var currentDateTime = DateTime.Now;

                        // Fetch today's parking bookings for the car number
                        var parkingList = await _context.CarParkings
                            .Where(p => p.carNumber == message.vehicleNo
                                && p.parkingStartTime.HasValue
                                && p.parkingStartTime.Value.Date == currentDateTime.Date)
                            .ToListAsync();

                        if (parkingList != null && parkingList.Count > 0)
                        {
                            // Check if current time is within any booking's parkingStartTime and parkingEndTime
                            var activeBooking = parkingList.FirstOrDefault(p =>
                                p.parkingStartTime.HasValue && p.parkingEndTime.HasValue &&
                                currentDateTime >= p.parkingStartTime.Value &&
                                currentDateTime <= p.parkingEndTime.Value);

                            if (activeBooking != null)
                            {
                                // Current time is inside booking time range -> open entry gate
                                PublishMessage("Gate", "EntryGateOpen");
                            }
                            else
                            {
                                // No active booking for current time, check other time conditions
                                // Look for any booking that starts in the future (later today)
                                var futureBooking = parkingList.FirstOrDefault(p => p.parkingStartTime.HasValue && currentDateTime < p.parkingStartTime.Value);

                                if (futureBooking != null)
                                {
                                    // Time before booking start
                                    PublishMessage("Errors", $"Error: Time will start on {futureBooking.parkingStartTime.Value.ToString("HH:mm")}");
                                }
                                else
                                {
                                    // If no future booking, then booking expired or passed
                                    var lastBooking = parkingList.OrderByDescending(p => p.parkingEndTime).FirstOrDefault();
                                    if (lastBooking != null && lastBooking.parkingEndTime.HasValue && currentDateTime > lastBooking.parkingEndTime.Value)
                                    {
                                        PublishMessage("Errors", "Booking expired");
                                    }
                                    else
                                    {
                                        // Fallback case: no valid booking found for current time
                                        PublishMessage("Errors", "No valid booking found for current time");
                                    }
                                }
                            }
                        }
                        else
                        {
                            // No booking found for the car today
                            PublishMessage("Errors", "NO Booking Found");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Failed to parse JSON: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating parking status: {ex.Message}");
                }
            }
            if (topic.Equals("CarExit", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var message = JsonConvert.DeserializeObject<VehicleParkingMQTT>(payload);
                    if (message == null || string.IsNullOrEmpty(message.vehicleNo) || string.IsNullOrEmpty(message.vehicleType))
                        return;

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var currentDateTime = DateTime.Now;

                        // Get latest booking for the vehicle
                        var lastBooking = await _context.CarParkings
                            .Where(p => p.carNumber == message.vehicleNo && p.parkingEndTime.HasValue)
                            .OrderByDescending(p => p.parkingEndTime)
                            .FirstOrDefaultAsync();

                        if (lastBooking != null)
                        {
                            if (currentDateTime <= lastBooking.parkingEndTime)
                            {
                                // Booking valid (exit is before or at end time)
                                PublishMessage("Gate", "ExitGateOpen");
                            }
                            else
                            {
                                // Booking time passed, calculate extra time and charge
                                TimeSpan extraTime = currentDateTime - lastBooking.parkingEndTime.Value;
                                double extraMinutes = extraTime.TotalMinutes;

                                // Example: Rs 2 per minute
                                double extraAmount = Math.Ceiling(extraMinutes) * 2;

                                string errorMsg = $"Please pay extra amount: Rs {extraAmount}";
                                PublishMessage("ExtraPayment", errorMsg);
                            }
                        }
                        else
                        {
                            PublishMessage("Errors", "No Booking Found");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Failed to parse JSON: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking exit status: {ex.Message}");
                }
            }





        }
        public async Task PublishMessage(string topic, string message)
        {
            var mqttFactory = new MqttClientFactory();
            string localIp = HelperMethods.GetLocalIpAddress();
            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(localIp) // or your custom broker
                .WithCredentials("admin", "admin123")
                .Build();

                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(message)
                    .Build();

                await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

                await mqttClient.DisconnectAsync();

                Console.WriteLine("MQTT application message is published.");
            }
        }

    }
}

