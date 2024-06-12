using System.Globalization;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using System.Runtime.InteropServices;

/// <summary>
/// The main program class that runs the Telegram bot.
/// </summary>
class Program
{
    private enum ConsoleCloseCtrlType
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }

    private delegate bool EventHandler(ConsoleCloseCtrlType signal);

    [DllImport("Kernel32")]
    private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
    private static EventHandler? handler;

    private static string settingsPath = "appsettings.json";
    private static string dataPath = "data.json";
    private static string subscribersPath = "subscribers.json";
    private static ITelegramBotClient? botClient;
    public static List<long>? subscribers = new();
    private static Timer? timer;
    private static CancellationTokenSource? cts;
    private static Dictionary<int, int>? recordsId = new(); // <tag_id, record_id>

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main()
    {
        if (System.IO.File.Exists(subscribersPath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(subscribersPath);
                subscribers = JsonConvert.DeserializeObject<List<long>>(json) ?? new List<long>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading subscribers: {ex.Message}");
                subscribers = new List<long>();
            }
        }
        else
        {
            Console.WriteLine("Subscribers file not found.");
            subscribers = new List<long>();
        }

        // Create configuration from the appsettings.json file
        var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(settingsPath, optional: false, reloadOnChange: true);
        var configuration = builder.Build();

        // Create a client for interacting with the Telegram API
        botClient = new TelegramBotClient(configuration["BotToken"]!);
        cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        // Start the process of receiving updates from Telegram
        botClient.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions, cancellationToken: cts.Token);
        Console.WriteLine($"Starting Bot");
        botClient!.SendTextMessageAsync(450844024, "Starting Bot");

        DateTime now = DateTime.Now;
        DateTime nextTrigger = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddMinutes(3).AddSeconds(30);

        while (nextTrigger < now)
        {
            nextTrigger = nextTrigger.AddSeconds(150);
        }

        TimeSpan initialDelay = nextTrigger - now;
        //TimeSpan initialDelay = TimeSpan.Zero;

        // Create a timer that will call the CallBack method every 150 seconds
        timer = new Timer(CallBack!, null, initialDelay, TimeSpan.FromSeconds(150));

        var consoleInputThread = new Thread(ListenForStopCommand);
        consoleInputThread.Start();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(handler, true);
        }
    }

    private static bool Handler(ConsoleCloseCtrlType signal)
    {
        SendShutdownMessage().Wait();
        return true;
    }

    /// <summary>
    /// Listens for the 'stop' command in the console input.
    /// </summary>
    private static void ListenForStopCommand()
    {
        while (true)
        {
            var input = Console.ReadLine();
            if (input != null && input.Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                cts?.Cancel();
                timer?.Dispose();
                SendShutdownMessage().Wait();
                break;
            }
        }
    }

    private async static Task SendShutdownMessage()
    {
        string shutdownMessage = "Bot is shutting down";
        foreach (var subscriber in subscribers!)
        {
            await botClient!.SendTextMessageAsync(subscriber, shutdownMessage);
        }
    }

    /// <summary>
    /// Handles updates from the Telegram bot.
    /// </summary>
    /// <param name="client">The Telegram bot client.</param>
    /// <param name="update">The update received from Telegram.</param>
    /// <param name="token">A cancellation token.</param>
    private static async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
    {
        Console.WriteLine($"[{update.Message!.Chat.Id}] Get Massage: " + update.Message!.Text ?? "[no text]");
        if (update.Message?.Text == "/start")
        {
            if (!subscribers!.Contains(update.Message.Chat.Id))
            {
                subscribers.Add(update.Message.Chat.Id);
                await client.SendTextMessageAsync(update.Message.Chat.Id, "You have subscribed to the alerts");

                using (StreamWriter file = System.IO.File.CreateText(subscribersPath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, subscribers);
                }
            }
            else
            {
                await client.SendTextMessageAsync(update.Message.Chat.Id, "You have already subscribed to the alerts");
            }
        }
        else
        if (update.Message?.Text == "/stop")
        {
            subscribers!.Remove(update.Message.Chat.Id);
            await client.SendTextMessageAsync(update.Message.Chat.Id, "You have unsubscribed from the alerts");

            using (StreamWriter file = System.IO.File.CreateText(subscribersPath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, subscribers);
            }
        }
        else
        if (update.Message?.Text == "/help")
        {
            await client.SendTextMessageAsync(update.Message.Chat.Id, "/start\n/stop\n/help");
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles errors from the Telegram bot.
    /// </summary>
    /// <param name="client">The Telegram bot client.</param>
    /// <param name="exception">The exception thrown.</param>
    /// <param name="token">A cancellation token.</param>
    private static async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        Console.WriteLine("Error: " + exception.Message);
        await Task.CompletedTask;
    }

    // <summary>
    /// Callback method that gets called periodically by the timer.
    /// </summary>
    /// <param name="state">The state object passed to the callback.</param>
    private static void CallBack(object state)
    {
        CheckAndSendAlerts();
    }

    /// <summary>
    /// Checks database for new records and sends alerts if criteria are met.
    /// </summary>
    private async static void CheckAndSendAlerts()
    {
        // Build configuration from appsettings.json file
        var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(settingsPath, optional: false, reloadOnChange: true);
        var configuration = builder.Build();

        // Get the connection string from the configuration
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        // Read database access details
        string json = System.IO.File.ReadAllText(dataPath);
        List<TagInfo>? tagInfoList = JsonConvert.DeserializeObject<List<TagInfo>>(json);

        // Open a connection to the MySQL database
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                int recordId = 0;
                float value = 0;
                string? plc_name = "";
                DateTime dateTime = DateTime.MinValue;

                // Open the database connection
                connection.Open();
                // Console.WriteLine("Connection successful!");
                // await botClient!.SendTextMessageAsync(450844024, "Connection to DB");

                // Iterate over each TagInfo object in the list
                foreach (var tagInfo in tagInfoList!)
                    // Iterate over each tag within the TagInfo object
                    foreach (var tag in tagInfo.tags!)
                    {
                        // Checking that there is an entry about the tag in the dictionary
                        if (!recordsId!.ContainsKey(tag.Value))
                        {
                            recordsId.Add(tag.Value, 0);
                        }

                        // Construct SQL query to retrieve the latest record for the tag
                        string request = @"SELECT * FROM record WHERE tag_id = @tag_id ORDER BY created_at DESC LIMIT 1";
                        MySqlCommand command = new MySqlCommand(request, connection);
                        command.Parameters.AddWithValue("@tag_id", tag.Value);

                        // Execute the SQL query
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            // Read the result set
                            while (await reader.ReadAsync())
                            {
                                dateTime = reader.GetDateTime("created_at");

                                // Retrieve the id of the lastest record
                                recordId = reader.GetInt32("id");

                                // Retrieve the timestamp of the latest record
                                if (float.TryParse(reader["value"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                                {
                                    value = result;
                                }
                            }
                        }

                        // Check if the value exceeds the criteria and if the record is recent
                        if ((value > tagInfo.criteria) && (recordsId[tag.Value] != recordId))
                        {
                            // Uodate recordsId
                            recordsId[tag.Value] = recordId;

                            // Construct SQL query to retrieve the PLC name
                            request = @"SELECT name FROM plc WHERE id = @plc_id";
                            command = new MySqlCommand(request, connection);
                            command.Parameters.AddWithValue("plc_id", tagInfo.plc_id);

                            // Execute the SQL query
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                // Read the result set
                                while (await reader.ReadAsync())
                                {
                                    // Retrieve the PLC name
                                    plc_name = reader["name"].ToString();
                                }
                            }

                            // Construct the output message
                            string outputString = $"{plc_name}. Ожидается {tag.Key}. Вероятность {value}%.";

                            // Log the output message
                            Console.WriteLine(outputString);

                            // Send the output message to all subscribers
                            foreach (var subscriber in subscribers!)
                                await botClient!.SendTextMessageAsync(subscriber, outputString);
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

