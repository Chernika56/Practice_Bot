using System.Data;
using System.Globalization;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

class Program
{
    private static string settingsPath = "appsettings.json";
    private static string dataPath = "data.json";
    private static ITelegramBotClient? botClient;
    public static List<long> subscribers = new();

    static void Main()
    {
        var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(settingsPath, optional: false, reloadOnChange: true);
        var configuration = builder.Build();

        botClient = new TelegramBotClient(configuration["BotToken"]!);
        var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions, cancellationToken: cts.Token);
        Console.WriteLine($"Starting Bot");

        Timer timer = new Timer(CallBack!, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));

        Console.ReadLine();
    }

    private static async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token) 
    {
        Console.WriteLine("Get Massage: " + update.Message?.Text ?? "[no text]");
        if (update.Message?.Text == "/start") 
        {
            subscribers.Add(update.Message.Chat.Id);
            await client.SendTextMessageAsync(update.Message.Chat.Id, "You have subscribed to the newsletter");
        }
        else 
        if (update.Message?.Text == "/stop") 
        {
            subscribers.Remove(update.Message.Chat.Id);
            await client.SendTextMessageAsync(update.Message.Chat.Id, "You have unsubscribed from the newsletter");
        }
        await Task.CompletedTask;
    }

    private static async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token) 
    {
        Console.WriteLine("Error: " + exception.Message);
        await Task.CompletedTask;
    }

    private static void CallBack(object state)
    {
        MyMethod();
    }

    private async static void MyMethod()
    {
        var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(settingsPath, optional: false, reloadOnChange: true);
        var configuration = builder.Build();

        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        string json = System.IO.File.ReadAllText(dataPath);
        List<TagInfo>? tagInfoList = JsonConvert.DeserializeObject<List<TagInfo>>(json);

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                float value = 0;
                string? plc_name = "";
                string? dateTime = "";

                connection.Open();
                Console.WriteLine("Connection successful!");

                foreach (var tagInfo in tagInfoList!)
                    foreach (var tag in tagInfo.tags!)
                    {
                        string request = @"SELECT value FROM record WHERE tag_id = @tag_id ORDER BY created_at DESC LIMIT 1";
                        MySqlCommand command = new MySqlCommand(request, connection);
                        command.Parameters.AddWithValue("@tag_id", tag.Value);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (float.TryParse(reader["value"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                                {
                                    value = result;
                                }
                            }
                        }

                        request = @"SELECT name FROM plc WHERE id = @plc_id";
                        command = new MySqlCommand(request, connection);
                        command.Parameters.AddWithValue("plc_id", tagInfo.plc_id);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                plc_name = reader["name"].ToString();
                            }
                        }

                        request = @"SELECT created_at FROM record WHERE tag_id = @tag_id AND value = @value";
                        command = new MySqlCommand(request, connection);
                        command.Parameters.AddWithValue("@tag_id", tag.Value);
                        command.Parameters.AddWithValue("@value", value);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                dateTime = reader["created_at"].ToString();
                            }
                        }

                        if (value > tagInfo.criteria)
                        {
                            foreach (var subscriber in subscribers)
                                await botClient!.SendTextMessageAsync(subscriber, $"{plc_name}: Expected {tag.Key} {value}, {dateTime}");
                            Console.WriteLine($"{plc_name}: Expected {tag.Key} {value}, {dateTime}");
                        }
                    }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}

