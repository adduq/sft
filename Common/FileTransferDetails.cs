using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SocketFileTransfer.Common;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FileTransferDetails
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
    private readonly string _fileName;

    private readonly long _fileSize;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    private readonly byte[] _securityCode;

    public FileTransferDetails(string fileName, long fileSize, byte[] securityCode)
    {
        if (fileName.Length > 100) throw new ArgumentException("File name cannot be longer than 100 characters.");
        if (securityCode.Length != 10) throw new ArgumentException("Security code must be 10 bytes long.");
        _fileName = fileName;
        _fileSize = fileSize;
        _securityCode = securityCode;
    }


    public string FileName => _fileName.Trim();
    public long FileSize => _fileSize;
    public byte[] SecurityCode => _securityCode;
    public static int Size => Marshal.SizeOf<FileTransferDetails>();
}