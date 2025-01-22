using CommandLine;
using SocketFileTransfer.Arguments;
using SocketFileTransfer.Client;
using SocketFileTransfer.Server;
using NLog;
using System.Net.Mail;

LogManager.Setup().LoadConfiguration(builder => {
    // Anything higher than logLevel will be written to console
    builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole(layout: "[${longdate}] ${message}");

    // Anything higher than Debug will be written to file with method name and line number
    builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToFile(fileName: "SocketFileTransfer.log",
        layout: "${longdate}|${level:uppercase=true}|${callsite}|${message}");
});

// Parse command line arguments
Parser.Default.ParseArguments<ClientOptions, ServerOptions>(args)
    .WithParsed<ClientOptions>(HandleForClient)
    .WithParsed<ServerOptions>(HandleForServer)
    .WithNotParsed(HandleParseError);

static void HandleForClient(ClientOptions opts)
{
    LogManager.GetCurrentClassLogger().Info("Send mode selected.");
    var client = new Client();

    Task.Run(() => client.StartAsync(opts.FileToSend, opts.Code)).Wait();
}
static void HandleForServer(ServerOptions opts)
{
    LogManager.GetCurrentClassLogger().Info("Receive mode selected.");
    var server = new Server();

    using var inactivityTimer = new Timer(_ => {
        var timeSinceLastActivity = DateTime.Now - server.LastActivityTime;
        if (!(timeSinceLastActivity.TotalMinutes > 5)) return;
        LogManager.GetCurrentClassLogger().Info("No activity for 5 minutes, shutting down...");
        Environment.Exit(0);
    }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    if (opts.Unsafe)
    {
        LogManager.GetCurrentClassLogger().Warn("WARNING: Unsafe mode selected, encryption will be disabled.");
    }

    Task.Run(() => server.RunAsync(opts.Port, opts.Directory, opts.IsLocal, !opts.Unsafe)).Wait();
}

static void HandleParseError(IEnumerable<Error> errs)
{
    var logger = LogManager.GetCurrentClassLogger();
    foreach (var error in errs)
    {
        logger.Error(error.ToString());
    }
}