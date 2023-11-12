using CommandLine;
using SocketFileTransfer.Common;

namespace SocketFileTransfer.Arguments;

[Verb("send", HelpText = "Run as sender.")]
public class ClientOptions
{
    [Value(0, HelpText = "File to send.", Required = true)]
    public string FileToSend { get; set; }
    
    [Value(1, Required = true, HelpText = "Host and port to connect to. Format is <host:port> or <alias>")]
    public string HostPort { get; set; }
    
    [Option('d', "directory", Default = "~", HelpText = "Directory to save the file to on the receiver.")]
    public string Directory { get; set; }
    
    [Option('p', "port", Default = Constants.DefaultListeningPort, HelpText = "Port to connect to.")]
    public ushort Port { get; set; }
    
}