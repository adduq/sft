using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using NLog;
using SocketFileTransfer.Arguments;

namespace SocketFileTransfer.Receiver;

public class Server
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly Socket _socket;
    private readonly ServerOptions _options;
     
    public Server(ServerOptions options)
    {
        _logger.Info("Receiver created");
        _options = options;
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }
    
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("Receiver started");
        _socket.Bind(new IPEndPoint(IPAddress.Any, 12345));
        _socket.Listen(_options.MaxConnections);

        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.Info("Waiting for a request...");
            var client = await _socket.AcceptAsync(cancellationToken);
            _logger.Info($"Client connected from {client.RemoteEndPoint}.");
            
            
            var key = await KeyExchange(client, cancellationToken);
        }
    }
    
    // Elliptic Curve Diffie-Hellman key exchange
    private async Task<byte[]> KeyExchange(Socket clientSocket, CancellationToken ct)
    {
        // Check if allowed to connect
        //... maybe read from allowed clients in config
        
        var server = ECDiffieHellman.Create();
        var ourPublicKeyBytes = server.PublicKey.ExportSubjectPublicKeyInfo();
        var theirPublicKeyBytes = new byte[ourPublicKeyBytes.Length];

        await clientSocket.ReceiveAsync(theirPublicKeyBytes, SocketFlags.None, ct);
        
        var client = ECDiffieHellman.Create();
        client.ImportSubjectPublicKeyInfo(theirPublicKeyBytes, out _);
        
        await clientSocket.SendAsync(ourPublicKeyBytes, SocketFlags.None, ct);
        
        return server.DeriveKeyMaterial(client.PublicKey);
    }


    public async Task StopAsync()
    {
        _logger.Info("Receiver stopped");
        await _socket.DisconnectAsync(true);
    }
}