namespace SocketFileTransfer.Common;

public static class FileUtils
{
    public static string GetReadableSize(decimal fileSizeBytes)
    {
        return fileSizeBytes switch
        {
            < 1024m => $"{fileSizeBytes} B",
            < 1024m * 1024m => $"{fileSizeBytes / 1024.0m:N2} KiB",
            < 1024m * 1024m * 1024m => $"{fileSizeBytes / 1024.0m / 1024.0m:N2} MiB",
            _ => $"{fileSizeBytes / 1024.0m / 1024.0m / 1024.0m:N2} GiB"
        };
    }
}