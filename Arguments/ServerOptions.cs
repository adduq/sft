using CommandLine;

#nullable disable
namespace SocketFileTransfer.Arguments;

// Also includes the ExtraOptions class
[Verb("receive", HelpText = "Run as receiver.")]
public class ServerOptions
{
    [Option('p', "port", Default = 55_000, HelpText = "Port to listen on.")]
    public ushort Port { get; set; }

    [Option('l', "local", Default = false, HelpText = "Run locally.")]
    public bool IsLocal { get; set; }

    [Option('u', "unsafe", Default = false, HelpText = "Disables encryption in exchange for higher transfer speeds.")]
    public bool Unsafe { get; set; }

    [Option('d', "directory", Default = ".", HelpText = "Directory to save the file to.")]
    public string Directory { get; set; }
}