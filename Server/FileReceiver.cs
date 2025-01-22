using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using NLog;
using System.Threading.Channels;
using SocketFileTransfer.Common;

namespace SocketFileTransfer.Server;

public delegate void ActivityEvent();

public class FileReceiver
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly Socket _client;
    private readonly byte[] _securityCode;
    private readonly bool _useEncryption;
    private FileTransferDetails _transferDetails;

    // For progress reporting
    private int _spinnerIdx;
    private static readonly string[] Spinner = { "...", "·..", ".·.", "..·" };
    private const char ProgressChar = '-';
    private long _totalBytesRead;
    private long _totalBytesLastSecond;
    private DateTime _lastSpeedUpdate;
    private readonly Queue<long> _bpsHistory;
    private const int MovingAverageWindow = 3;
    private double _peakBps;
    private long _lwmaBps; // Linearly weighted moving average
    private TimeSpan _estimatedTimeRemaining;
    private Timer? _progressTimer;

    public FileReceiver(Socket client, byte[] securityCode, bool useEncryption)
    {
        _client = client;
        _securityCode = securityCode;
        _useEncryption = useEncryption;
        _bpsHistory = new Queue<long>();
    }

    public event ActivityEvent NotifyActive = delegate { };

    public async Task BeginFileTransferAsync()
    {
        var receiveBuffer = new Memory<byte>(new byte[65536]);
        await using var stream = new NetworkStream(_client, true);
        var channel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        int bytesRead;

        await InitTransfer(stream);
        var writeTask = WriteToFileAsync(channel.Reader);
        _progressTimer = new Timer(ReportProgress, null, 200, 200);
        var sw = Stopwatch.StartNew();

        while ((bytesRead = await stream.ReadAsync(receiveBuffer)) > 0)
        {
            await channel.Writer.WriteAsync(receiveBuffer[..bytesRead].ToArray());

            _totalBytesRead += bytesRead;
            _totalBytesLastSecond += bytesRead;
        }
        sw.Stop();
        // Report progress one last time, to show 100% completion then stop the timer
        await _progressTimer.DisposeAsync();
        // Clear the progress line
        Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
        var avgMiBps = $"{FileUtils.GetReadableSize((decimal)(_totalBytesRead / sw.Elapsed.TotalSeconds))}/s";
        _logger.Info($"Transfer completed in {sw.Elapsed.TotalSeconds:N2} seconds (avg. {avgMiBps}, peak {FileUtils.GetReadableSize((decimal)_peakBps)}/s)");

        // Mark the writer channel as completed, indicating that no more data will be sent
        channel.Writer.Complete();

        // Wait for the writer task to finish writing to the file
        _logger.Info("Saving file...");
        await writeTask;
        Console.WriteLine();
    }

    private async Task InitTransfer(NetworkStream stream)
    {
        // 10 bytes for security code,
        // 8 bytes for file size,
        // 100 bytes for file name,
        // 1 byte for transfer type
        // Total: 119 bytes

        var transferDetailsBytes = new byte[FileTransferDetails.Size];
        await stream.ReadExactlyAsync(transferDetailsBytes, 0, transferDetailsBytes.Length);
        var transferDetails = StructUtils.FromByteArray<FileTransferDetails>(transferDetailsBytes);

        // Security code is the first 10 bytes of the buffer
        if (!transferDetails.SecurityCode.SequenceEqual(_securityCode))
        {
            _logger.Error("Invalid transfer code, exiting...");

            // Gracefully close the connection
            await _client.SendAsync(new ArraySegment<byte>(new byte[] { 0 }), SocketFlags.None);
            _client.Shutdown(SocketShutdown.Both);
            _client.Close();
            Environment.Exit(1);
        }
        else
        {
            await _client.SendAsync(new ArraySegment<byte>(new byte[] { 1 }), SocketFlags.None);
        }
        _transferDetails = transferDetails;
        _logger.Info($"File transfer started for {_transferDetails.FileName} ({FileUtils.GetReadableSize(_transferDetails.FileSize)})");
    }

    private async Task WriteToFileAsync(ChannelReader<byte[]> reader)
    {
        if (_transferDetails.Equals(default)) throw new InvalidOperationException("Transfer details not initialized");

        await using var writer = File.OpenWrite(_transferDetails.FileName);
        Stream outputStream = writer;


        if (_useEncryption)
        {
            var rfc = new Rfc2898DeriveBytes(_transferDetails.SecurityCode, Array.Empty<byte>(), 50_000, HashAlgorithmName.SHA512);
            var key = rfc.GetBytes(32); // 256 bits (AES-256 key size)
            var iv = rfc.GetBytes(16); // 128 bits (AES block size)
            var aes = Aes.Create();
            var decryptor = aes.CreateDecryptor(key, iv);

            outputStream = new CryptoStream(outputStream, decryptor, CryptoStreamMode.Write);
        }

        while (await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out var chunk))
            {
                outputStream.Write(chunk);
            }
        }

        outputStream.Close();
        _logger.Info("File saved successfully.");
    }

    private void ReportProgress(object? _)
    {
        if ((DateTime.Now - _lastSpeedUpdate).TotalSeconds > 1)
        {
            if (_totalBytesLastSecond > _peakBps) _peakBps = _totalBytesLastSecond;
            _bpsHistory.Enqueue(_totalBytesLastSecond);
            if (_bpsHistory.Count > MovingAverageWindow) _bpsHistory.Dequeue();
            _lwmaBps = _bpsHistory.Select((bps, i) => bps * (i + 1)).Sum() / (_bpsHistory.Count * (_bpsHistory.Count + 1) / 2);
            _estimatedTimeRemaining = _lwmaBps == 0 ? TimeSpan.MaxValue : TimeSpan.FromSeconds((double)(_transferDetails.FileSize - _totalBytesRead) / _lwmaBps);
            _totalBytesLastSecond = 0;
            _lastSpeedUpdate = DateTime.Now;
            NotifyActive();
        }
        if (_lwmaBps == 0) return;
        var percent = decimal.Divide(_totalBytesRead, _transferDetails.FileSize) * 100;
        var progress = new string(ProgressChar, (int)(percent / 2)).PadRight(50);
        _spinnerIdx %= Spinner.Length;
        var status = $"Receiving file{Spinner[_spinnerIdx]}";
        Console.Write($"\r{status} [{progress}] ({percent:N2}%) @ {FileUtils.GetReadableSize(_lwmaBps)}/s ETA {_estimatedTimeRemaining:hh\\:mm\\:ss\\.ff}");
        _spinnerIdx++;
    }
}