using CommandLine;

#nullable disable
namespace SocketFileTransfer.Arguments;

[Verb("send", HelpText = "Run as sender.")]
public class ClientOptions
{
    [Option('f', "file", HelpText = "File to send.", Required = true)]
    public string FileToSend { get; set; }

    [Option('c', "code", HelpText = "Code that is given by the receiver.", Required = true)]
    public string Code { get; set; }
}