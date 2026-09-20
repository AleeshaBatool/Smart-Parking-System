using MQTTnet;
using MQTTnet.Server;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SmartParkingSystem.Helper
{
    public static class MqttServerExtensions
    {
        //public static void UseCarParkingMessageHandler(this IMqttServer mqttServer)
        //{
        //    mqttServer.ApplicationMessageReceivedAsync += e =>
        //    {
        //        var topic = e.ApplicationMessage.Topic;
        //        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload ?? Array.Empty<byte>());

        //        if (topic.Equals("carparking", StringComparison.OrdinalIgnoreCase))
        //        {
        //            try
        //            {
        //                var message = JsonConvert.DeserializeObject<CarParkingMessage>(payload);
        //                Console.WriteLine($"Received parking data:");
        //                Console.WriteLine($"Vehicle No: {message.vechileno}");
        //                Console.WriteLine($"Vehicle Type: {message.vehicleType}");
        //                Console.WriteLine($"Parking Name: {message.parkingName}");

        //                // TODO: Add logic to process the message, e.g., save to database
        //            }
        //            catch (JsonException ex)
        //            {
        //                Console.WriteLine($"Failed to parse JSON: {ex.Message}");
        //            }
        //        }

        //        return Task.CompletedTask;
        //    };
        //}
    }
}
