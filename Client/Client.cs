using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using NLog;
using SocketFileTransfer.Common;

namespace SocketFileTransfer.Client;

public class Client
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public async Task StartAsync(string filePath, string code)
    {
        // Check if file or directory exists
        if (!File.Exists(filePath) && !Directory.Exists(filePath))
        {
            _logger.Error("File or directory does not exist.");
            return;
        }

        // Add padding to base64 code if needed
        if (code.Length % 4 != 0) code += new string('=', 4 - code.Length % 4);
        var transferCodeBytes = Convert.FromBase64String(code);
        var connection = StructUtils.FromByteArray<ConnectionDetails>(transferCodeBytes);

        try
        {
            if (File.Exists(filePath))
                await SendFileAsync(
                    filePath,
                    connection);
            else
                _logger.Error($"File {filePath} does not exist.");
        }
        catch (SocketException e)
        {
            _logger.Error($"SocketException: {e.Message}");
        }
        catch (Exception e)
        {
            _logger.Error($"Exception: {e.Message}");
        }
    }

    private async Task SendFileAsync(string filePath, ConnectionDetails connectionDetails)
    {
        var fileName = Path.GetFileName(filePath);
        var fileSize = new FileInfo(filePath).Length;
        var transferDetails = new FileTransferDetails(fileName, fileSize, connectionDetails.SecurityCode);

        // Create a new socket
        _logger.Info($"Connecting to {connectionDetails.IpAddress}:{connectionDetails.Port}...");
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(new IPEndPoint(connectionDetails.IpAddress, connectionDetails.Port));
        await ValidateWithReceiver(socket, transferDetails.AsByteArray());

        // Send file
        _logger.Info($"Sending {fileName} ({FileUtils.GetReadableSize(fileSize)})");
        await using var fileStream = File.OpenRead(filePath);
        await using var networkStream = new NetworkStream(socket);

        Stream outputStream = networkStream;

        if (connectionDetails.UseEncryption) // Encrypt file data when sending
        {
            var rfc = new Rfc2898DeriveBytes(connectionDetails.SecurityCode, Array.Empty<byte>(), 50_000, HashAlgorithmName.SHA512);
            var key = rfc.GetBytes(32);
            var iv = rfc.GetBytes(16);

            var aesProvider = Aes.Create();
            var encryptor = aesProvider.CreateEncryptor(key, iv);

            outputStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
        }

        await fileStream.CopyToAsync(outputStream);
        outputStream.Close();

        _logger.Info("File sent successfully.");
    }

    private async Task ValidateWithReceiver(Socket socket, byte[] buffer)
    {
        var ackBuffer = new byte[1];
        await socket.SendAsync(buffer, SocketFlags.None);
        await socket.ReceiveAsync(ackBuffer, SocketFlags.None);
        if (ackBuffer[0] == 0)
        {
            _logger.Error("Server rejected security code.");
            Environment.Exit(1);
        }

        // Security code accepted, begin file transfer
        _logger.Info("Security code accepted.");
    }
}