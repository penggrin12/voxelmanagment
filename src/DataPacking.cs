namespace Game;

using System;

// made by chatgpt, god bless
public static class DataPacking
{
    public static ulong PackData(byte b1, byte b2, byte b3, short i1, short i2)
    {
        byte[] data = new byte[8];

        data[0] = b1;
        data[1] = b2;
        data[2] = b3;

        Array.Copy(BitConverter.GetBytes(i1), 0, data, 3, 2);
        Array.Copy(BitConverter.GetBytes(i2), 0, data, 5, 2);

        return BitConverter.ToUInt64(data, 0);
    }

    public static void UnpackData(ulong packed, out byte b1, out byte b2, out byte b3, out short i1, out short i2)
    {
        byte[] data = BitConverter.GetBytes(packed);

        b1 = data[0];
        b2 = data[1];
        b3 = data[2];

        i1 = BitConverter.ToInt16(data, 3);
        i2 = BitConverter.ToInt16(data, 5);
    }
}

