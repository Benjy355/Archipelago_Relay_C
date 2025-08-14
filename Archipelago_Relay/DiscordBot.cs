using Archipelago;
using Discord;
using Discord.WebSocket;
using System.Net.Http.Headers;

public static class DiscordBot
{
#if DEBUG
    public const LogSeverity Global_Logging_Level = LogSeverity.Debug;
#else
    public const LogSeverity Global_Logging_Level = LogSeverity.Warning;
#endif

    public static string DiscordOathToken { get; set; } = "";

    public static DiscordSocketClient Client { get; } = new();

    static DiscordBot()
    {
        
    }

    public static Task Log(Exception exception, string source = "Misc", LogSeverity logLevel = LogSeverity.Error)
    {
        string message = $"Exception occurred: {exception.Message}\nStackTrace: {exception.StackTrace}";
        return Log(new LogMessage(logLevel, source, message));
    }

    public static Task Log(string message, string source = "Misc", LogSeverity logLevel = LogSeverity.Info)
    {
        return Log(new LogMessage(logLevel, source, message));
    }

    public static Task Log(LogMessage message)
    {
        if (message.Severity <= Global_Logging_Level)
        {
            Console.WriteLine(message);
        }
        return Task.CompletedTask;
    }

    public static async Task botLoop()
    {
        while (true) { 
            await Task.Delay(5); // CPU schedulers hate this one trick
        }
    }

    public static async Task main()
    {
        Client.Log += Log;

        var clientConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildScheduledEvents & ~GatewayIntents.GuildInvites
        };

        try
        {
            DiscordOathToken = File.ReadAllText("my_token.txt");
        }
        catch (Exception ex)
        {
            await Log($"Failed to read token from my_token.txt: {ex.Message}", "DiscordBot", LogSeverity.Critical);
            return;
        }

        // Hook in our event functions
        Client.Ready += ClientReadyCallback;
        Client.SlashCommandExecuted += SlashCommandCallback;

        // Connect
        await Client.LoginAsync(Discord.TokenType.Bot, DiscordOathToken);
        await Client.StartAsync();
        await botLoop();

        await Task.CompletedTask;
    }

    // Event functions
    public static async Task ClientReadyCallback()
    {
        // Set up our slash commands
        SlashCommandBuilder cmd_connect = new();
        cmd_connect.WithName("connect");
        cmd_connect.WithDescription("Connect to a game using the Archipelago.gg link");
        cmd_connect.AddOption("room_link", ApplicationCommandOptionType.String, "The Archipelago.gg room link to connect to", true);


        // Register our commands
        await Client.CreateGlobalApplicationCommandAsync(cmd_connect.Build());
    }
    public static async Task SlashCommandCallback(SocketSlashCommand cmd)
    {
        switch (cmd.Data.Name)
        {
            case "connect":
                await Log("Connect Command Called", "DiscordBot", LogSeverity.Debug);
                await handle_connect(cmd);
                break;
            default:
                await Log("Unknown command: " + cmd.Data.Name, "DiscordBot", LogSeverity.Error);
                break;
        }
    }

    // Slash Command functions
    public static async Task handle_connect(SocketSlashCommand cmd)
    {
        await cmd.DeferAsync(ephemeral: true);
        await cmd.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = "Please wait...";
        });
        Archipelago.GameData GameData = await Archipelago.SiteScraper.LoadSiteData(cmd.Data.Options.First().Value.ToString());
        if (GameData == null)
        {
            await cmd.ModifyOriginalResponseAsync(
                msg =>
                {
                    msg.Content = "Failed to load game data. Please check the link and try again.";
                });
            return;
        }
        await cmd.ModifyOriginalResponseAsync(
            msg =>
            {
                msg.Content = "Connecting...";
            });
        Relay relay = new(GameData);
        Task.Run(() => relay.Connect());
    }
}
