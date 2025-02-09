using System;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using Bogus;

class FakeOrderGenerator
{
    private static readonly ClientWebSocket _webSocket = new ClientWebSocket();
    private static readonly Uri _serverUri = new Uri("wss://localhost:7121/wss/orders");
    private static readonly Random _random = new Random();

    /// <summary>
    /// Main method to connect to WebSocket server and generate fake orders.
    /// </summary>
    /// <returns>Task representing the asynchronous operation.</returns>
    public static async Task Main()
    {
        try
        {
            await _webSocket.ConnectAsync(_serverUri, CancellationToken.None);
            Console.WriteLine("Connected to WebSocket server. Generating orders...");

            var orderFaker = new Faker<Order>()
            .RuleFor(o => o.Id, f => 3)
            .RuleFor(o => o.CustomerName, f => f.Name.FullName())
            .RuleFor(o => o.Timestamp, f => DateTime.Now)
            .RuleFor(o => o.Users_id, f => f.Random.Number(1, 10));

            while (_webSocket.State == WebSocketState.Open)
            {
                var order = orderFaker.Generate();
                string jsonOrder = JsonConvert.SerializeObject(order);
                byte[] buffer = Encoding.UTF8.GetBytes(jsonOrder);

                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine($"Sent order {order.Id} at {order.Timestamp}");

                int delay = _random.Next(3000, 8000); // Random delay between 3-8 seconds
                await Task.Delay(delay);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

/// <summary>
/// Represents an order.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public DateTime Timestamp { get; set; }
    public int Users_id { get; set; }
}
