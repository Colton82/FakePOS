using System;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using System.Collections.Generic;

class FakeOrderGenerator
{
    private static readonly ClientWebSocket _webSocket = new ClientWebSocket();
    private static readonly Uri _serverUri = new Uri("wss://localhost:7121/wss/pos");
    private static readonly Random _random = new Random();
    private static bool _autoGenerate = false; // Toggle auto-generation
    private static bool _exit = false; // Exit flag
    private static int userID;

    public static async Task Main()
    {
        try
        {
            await _webSocket.ConnectAsync(_serverUri, CancellationToken.None);
            Console.WriteLine("Connected to WebSocket server.");
            Console.WriteLine("Enter the user ID: ");
            userID = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Press 'S' to send a single order, 'T' to toggle auto-generation, or 'Q' to quit.");

            // Start listening for user input
            Task.Run(() => ListenForUserInput());

            var orderFaker = new Faker<Order>()
                .RuleFor(o => o.Id, f => f.IndexFaker.ToString())
                .RuleFor(o => o.CustomerName, f => f.Name.FullName())
                .RuleFor(o => o.Timestamp, f => DateTime.Now)
                .RuleFor(o => o.Users_id, f => userID)
                .RuleFor(o => o.Items, f => GenerateRandomItems()); // Assign random items

            while (!_exit)
            {
                if (_autoGenerate)
                {
                    await SendOrder(orderFaker.Generate());
                    int delay = _random.Next(3000, 8000); // Random delay between 3-8 seconds
                    await Task.Delay(delay);
                }
                else
                {
                    await Task.Delay(500); // Prevent CPU overuse when waiting for input
                }
            }

            Console.WriteLine("Exiting Fake Order Generator...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Listens for user inputs to send orders or toggle auto-generation.
    /// </summary>
    private static void ListenForUserInput()
    {
        while (!_exit)
        {
            var key = Console.ReadKey(true).Key;
            switch (key)
            {
                case ConsoleKey.S:
                    SendOrder(new Faker<Order>()
                        .RuleFor(o => o.Id, f => f.UniqueIndex.ToString())
                        .RuleFor(o => o.CustomerName, f => f.Name.FullName())
                        .RuleFor(o => o.Timestamp, f => DateTime.Now)
                        .RuleFor(o => o.Users_id, f => userID)
                        .RuleFor(o => o.Items, f => GenerateRandomItems()) // Random items
                        .Generate()).Wait();
                    Console.WriteLine("Sent one order.");
                    break;

                case ConsoleKey.T:
                    _autoGenerate = !_autoGenerate;
                    Console.WriteLine($"Auto-generation toggled: {(_autoGenerate ? "ON" : "OFF")}");
                    break;

                case ConsoleKey.Q:
                    _exit = true;
                    break;
            }
        }
    }

    /// <summary>
    /// Sends an order via WebSocket and prints the JSON.
    /// </summary>
    /// <param name="order">The order to send.</param>
    private static async Task SendOrder(Order order)
    {
        try
        {
            string jsonOrder = JsonConvert.SerializeObject(order, Formatting.Indented);
            Console.WriteLine("Generated Order JSON:\n" + jsonOrder);

            byte[] buffer = Encoding.UTF8.GetBytes(jsonOrder);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

            Console.WriteLine($"Sent order {order.Id} at {order.Timestamp}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send order: {ex.Message}");
        }
    }



    /// <summary>
    /// Generates a random set of food items for an order.
    /// </summary>
    /// <returns>A list of order items.</returns>
    private static List<OrderItem> GenerateRandomItems()
    {
        var items = new List<OrderItem>();

        var foodOptions = new List<string> { "Burger", "Pizza", "Shake", "Salad", "Pasta" };
        int itemCount = _random.Next(1, 4); // 1 to 3 items per order

        for (int i = 0; i < itemCount; i++)
        {
            string foodItem = foodOptions[_random.Next(foodOptions.Count)];
            items.Add(new OrderItem
            {
                Name = foodItem,
                Properties = GenerateItemDetails(foodItem)
            });
        }

        return items;
    }

    /// <summary>
    /// Generates dynamic attributes for different food items.
    /// </summary>
    /// <param name="foodType">Type of food item.</param>
    /// <returns>Dictionary of attributes for the item.</returns>
    private static List<ItemProperty> GenerateItemDetails(string foodType)
    {
        var properties = new List<ItemProperty>();

        switch (foodType)
        {
            case "Burger":
                properties.Add(new ItemProperty("Size", _random.Next(1, 3) == 1 ? "Small" : "Large"));
                properties.Add(new ItemProperty("Extras", new List<string> { "Cheese", "Bacon", "Lettuce" }[_random.Next(3)]));
                break;

            case "Pizza":
                properties.Add(new ItemProperty("Size", new List<string> { "Small", "Medium", "Large" }[_random.Next(3)]));
                properties.Add(new ItemProperty("Toppings", new List<string> { "Pepperoni", "Mushrooms", "Olives" }[_random.Next(3)]));
                properties.Add(new ItemProperty("Crust", _random.Next(1, 3) == 1 ? "Thin" : "Thick"));
                break;

            case "Shake":
                properties.Add(new ItemProperty("Flavor", new List<string> { "Chocolate", "Vanilla", "Strawberry" }[_random.Next(3)]));
                properties.Add(new ItemProperty("Size", new List<string> { "Small", "Medium", "Large" }[_random.Next(3)]));
                break;

            case "Salad":
                properties.Add(new ItemProperty("Dressing", new List<string> { "Ranch", "Caesar", "Balsamic" }[_random.Next(3)]));
                properties.Add(new ItemProperty("Protein", new List<string> { "Chicken", "Tofu", "None" }[_random.Next(3)]));
                break;

            case "Pasta":
                properties.Add(new ItemProperty("Type", new List<string> { "Spaghetti", "Fettuccine", "Penne" }[_random.Next(3)]));
                properties.Add(new ItemProperty("Sauce", new List<string> { "Marinara", "Alfredo", "Pesto" }[_random.Next(3)]));
                break;

            default:
                properties.Add(new ItemProperty("Custom", "Unknown item"));
                break;
        }

        return properties;
    }


}

/// <summary>
/// Represents an order.
/// </summary>
/// <summary>
/// Represents an order.
/// </summary>
public class Order
{
    public string Id { get; set; }
    public string CustomerName { get; set; }
    public DateTime Timestamp { get; set; }
    public int Users_id { get; set; }
    public List<OrderItem> Items { get; set; } // Updated to a list
}

/// <summary>
/// Represents a food item in an order.
/// </summary>
public class OrderItem
{
    public string Name { get; set; }
    public List<ItemProperty> Properties { get; set; } = new List<ItemProperty>();
}

/// <summary>
/// Represents a property of a food item.
/// </summary>
public class ItemProperty
{
    public string Key { get; set; }
    public string Value { get; set; }

    // Add Constructor that accepts two parameters
    public ItemProperty(string key, string value)
    {
        Key = key;
        Value = value;
    }
}


