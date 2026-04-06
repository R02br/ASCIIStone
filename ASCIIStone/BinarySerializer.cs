using System.Text;
using static Packet;

static class BinarySerializer
{
    public static void WriteByte(ref List<byte> buffer, byte value)
    {
        buffer.Add(value);
    }

    public static short ReadByte(byte[] buffer, ref int index)
    {
        index += sizeof(byte);
        return buffer[index - sizeof(byte)];
    }

    public static void WriteShort(ref List<byte> buffer, short value)
    {
        buffer.AddRange(BitConverter.GetBytes(value));
    }

    public static short ReadShort(byte[] buffer, ref int index)
    {
        index += sizeof(short);
        return BitConverter.ToInt16(buffer, index - sizeof(short));
    }

    public static void WriteInt(ref List<byte> buffer, int value)
    {
        buffer.AddRange(BitConverter.GetBytes(value));
    }

    public static int ReadInt(byte[] buffer, ref int index)
    {
        index += sizeof(int);
        return BitConverter.ToInt32(buffer, index - sizeof(int));
    }

    public static void WriteFloat(ref List<byte> buffer, float value)
    {
        buffer.AddRange(BitConverter.GetBytes(value));
    }

    public static float ReadFloat(byte[] buffer, ref int index)
    {
        index += sizeof(float);
        return BitConverter.ToSingle(buffer, index - sizeof(float));
    }

    public static void WriteString(ref List<byte> buffer, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        WriteShort(ref buffer, (short)bytes.Length);
        buffer.AddRange(bytes);
    }

    public static string ReadString(byte[] buffer, ref int index)
    {
        int stringLength = ReadShort(buffer, ref index);
        index += stringLength;
        return Encoding.UTF8.GetString((buffer.AsMemory(index - stringLength, stringLength)).ToArray());
    }
}