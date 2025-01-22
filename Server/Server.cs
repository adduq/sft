using System.Net;
using System.Net.Sockets;
using NLog;
using SocketFileTransfer.Common;

namespace SocketFileTransfer.Server;

public class Server
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public Server()
    {
        LastActivityTime = DateTime.Now;
    }

    public DateTime LastActivityTime { get; private set; }

    public async Task RunAsync(ushort portNumber, string directory, bool runLocally, bool useEncryption)
    {
        Socket? server = null;

        // Check if valid directory
        if (!Directory.Exists(directory))
        {
            _logger.Error("Directory does not exist, creating...");
            Directory.CreateDirectory(directory);
        }
        Directory.SetCurrentDirectory(directory);

        try
        {
            var localAddr = IPAddress.Any;
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(localAddr, portNumber));
            server.Listen();

            // Enter the listening loop.
            while (true)
            {
                _logger.Info("Waiting for a transfer request...");
                var (transferCode, securityCode) = await GetTransferCode(portNumber, runLocally, useEncryption);
                _logger.Warn($"TRANSFER CODE: {transferCode}");
                using var client = await server.AcceptAsync();
                _logger.Info($"Client connected from {client.RemoteEndPoint}.");
                LastActivityTime = DateTime.Now;
                var fileReceiver = new FileReceiver(client, securityCode, useEncryption);
                fileReceiver.NotifyActive += () => LastActivityTime = DateTime.Now;
                try
                {
                    await fileReceiver.BeginFileTransferAsync();
                }
                catch (SocketException e)
                {
                    _logger.Error($"SocketException: {e.Message}");
                }
                catch (Exception e)
                {
                    _logger.Error($"Exception: {e}");
                }
                client.Close();
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }
        finally
        {
            server?.Close();
        }
    }

    private static async Task<(string, byte[])> GetTransferCode(ushort port, bool runLocally, bool useEncryption)
    {
        var ip = runLocally ? IPAddress.Loopback : IPAddress.Parse(
            (await new HttpClient().GetStringAsync("http://ip-api.com/line?fields=query")).Trim());

        var ipToInt = BitConverter.ToUInt32(ip.GetAddressBytes());

        var details = new ConnectionDetails(ipToInt, port, useEncryption);

        return (details.ToBase64String().Replace("=", ""), details.SecurityCode);
    }
}