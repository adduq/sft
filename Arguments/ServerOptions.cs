using CommandLine;

namespace SocketFileTransfer.Arguments;

[Verb("receive", HelpText = "Run as receiver." )]
public class ServerOptions
{
    [Option('M', "max-connections", Default = 1, HelpText = "Maximum number of connections to accept.")]
    public int MaxConnections { get; set; }
}
