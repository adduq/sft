using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using NLog;
using SocketFileTransfer.Arguments;

namespace SocketFileTransfer.Client;

public class Client
{
    private readonly ClientOptions _options;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    public Client(ClientOptions options){
        _options = options;
    }
    
    public async Task RunAsync()
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(IPAddress.Loopback, 12345);
        
        var aesKey = await KeyExchange(socket);
    }

    // Elliptic Curve Diffie-Hellman key exchange
    private async Task<byte[]> KeyExchange(Socket socket)
    {
        // Send public key
        var client = ECDiffieHellman.Create();
        var publicKeyBytes = client.PublicKey.ExportSubjectPublicKeyInfo();
        await socket.SendAsync(publicKeyBytes);
        
        // Receive the server's public key
        var serverPubKeyBytes = new byte[1024];
        await socket.ReceiveAsync(serverPubKeyBytes);
        
        // Compute the shared key
        var server = ECDiffieHellman.Create();
        server.ImportSubjectPublicKeyInfo(serverPubKeyBytes, out _);
        return client.DeriveKeyMaterial(server.PublicKey);
    }
}