using System.Runtime.InteropServices;

namespace SocketFileTransfer.Common;

public static class StructUtils
{
    public static byte[] AsByteArray<T>(this T @struct) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var bytes = new byte[size];
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(@struct, ptr, true);
        Marshal.Copy(ptr, bytes, 0, size);
        Marshal.FreeHGlobal(ptr);
        return bytes;
    }

    public static T FromByteArray<T>(byte[] data) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        if (data.Length != size) throw new ArgumentException($"Data must be {size} bytes long.");
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(data, 0, ptr, size);
        var packet = Marshal.PtrToStructure<T>(ptr);
        Marshal.FreeHGlobal(ptr);
        return packet;
    }

    public static string ToBase64String<T>(this T @struct) where T : struct
    {
        return Convert.ToBase64String(@struct.AsByteArray());
    }
}