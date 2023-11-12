using CommandLine;
using NLog;
using SocketFileTransfer.Arguments;
using SocketFileTransfer.Client;
using SocketFileTransfer.Receiver;
using ConfigurationManager = SocketFileTransfer.Configuration.ConfigurationManager;

LogManager.Setup().LoadConfiguration(builder => {
    // Anything higher than logLevel will be written to console
    builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole(layout:"[${longdate}] ${message}");
            
    // Anything higher than Debug will be written to file with method name and line number
    builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToFile(fileName: "SocketFileTransfer.log",
        layout: "${longdate}|${level:uppercase=true}|${callsite}|${message}");
});

// Load configuration
ConfigurationManager.Instance.Load("config.json");

// Parse command line arguments
await Parser.Default.ParseArguments<ClientOptions, ServerOptions>(args)
    .MapResult(
        (ClientOptions opts) => new Client(opts).RunAsync(),
        (ServerOptions opts) => new Server(opts).RunAsync(),
        HandleParseErrors);


static Task HandleParseErrors(IEnumerable<Error> errs)
{
    var logger = LogManager.GetCurrentClassLogger();
    foreach (var error in errs)
    {
        logger.Error(error.ToString());
    }

    return Task.CompletedTask;
}