using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SocketFileTransfer.Common;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ConnectionDetails
{
    private readonly uint _ipAddress;
    private readonly ushort _port;

    // bit 0: encryption (0 = no encryption, 1 = encryption)
    // bit 1-7: free
    private readonly byte _flags;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    private readonly byte[] _securityCode;


    public ConnectionDetails(uint ipAddress, ushort port, bool useEncryption)
    {
        _ipAddress = ipAddress;
        _port = port;
        _securityCode = new byte[10];

        if (useEncryption) _flags |= 0b00000001;

        // Generate random security code which is cryptographically secure
        RandomNumberGenerator.Create().GetBytes(_securityCode);
    }

    public IPAddress IpAddress => new(_ipAddress);

    public ushort Port => _port;

    public byte[] SecurityCode => _securityCode;

    public bool UseEncryption => (_flags & 0b0000_0001) == 0b00000001;
}